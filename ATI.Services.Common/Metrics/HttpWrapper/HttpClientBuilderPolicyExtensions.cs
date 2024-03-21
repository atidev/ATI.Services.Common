using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Options;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Polly;
using Polly.CircuitBreaker;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Registry;
using Polly.Timeout;

namespace ATI.Services.Common.Metrics.HttpWrapper;

public static class HttpClientBuilderPolicyExtensions
{
    /// <summary>
    /// Func, чтобы на каждый запрос он расчитывался по-новой и давал разные временные интервалы ретрая
    /// </summary>
    private static readonly Func<int, IEnumerable<TimeSpan>> RetryPolicyDelay =  retryCount => Backoff.DecorrelatedJitterBackoffV2(
        medianFirstRetryDelay: TimeSpan.FromSeconds(1),
        retryCount: retryCount);

    public static IHttpClientBuilder AddRetryPolicy(
        this IHttpClientBuilder clientBuilder,
        BaseServiceOptions serviceOptions,
        ILogger logger)
    {
        return clientBuilder
            .AddPolicyHandler((_, message) =>
            {
                // Ретраим только GET и DELETE запросы, так как они идемпотентны
                if (message.Method != HttpMethod.Get && message.Method != HttpMethod.Delete)
                    return Policy.NoOpAsync<HttpResponseMessage>();

                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<TimeoutRejectedException>()
                    .OrResult(r => r.StatusCode == (HttpStatusCode) 429) // Too Many Requests
                    .WaitAndRetryAsync(RetryPolicyDelay(serviceOptions.RetryCount),
                        (response, sleepDuration, retryCount,_) =>
                        {
                            logger.ErrorWithObject(response?.Exception, "Ошибка при WaitAndRetry",  new
                            {
                                message.RequestUri,
                                message.Method,
                                response.Result.StatusCode,
                                sleepDuration,
                                retryCount
                            } );
                        });
            });
    }

    public static IHttpClientBuilder AddHostSpecificCircuitBreakerPolicy(
        this IHttpClientBuilder clientBuilder,
        BaseServiceOptions serviceOptions,
        ILogger logger)
    {
        var registry = new PolicyRegistry();
        return clientBuilder.AddPolicyHandler(message =>
        {
            var policyKey = message.RequestUri.Host;
            var policy = registry.GetOrAdd(policyKey, BuildCircuitBreakerPolicy(message, serviceOptions, logger));
            return policy;
        });
    }
    
    public static IHttpClientBuilder AddTimeoutPolicy(
        this IHttpClientBuilder httpClientBuilder,
        TimeSpan timeout)
    {
        return httpClientBuilder.AddPolicyHandler(Policy.TimeoutAsync<HttpResponseMessage>(timeout));
    }

    private static AsyncCircuitBreakerPolicy<HttpResponseMessage> BuildCircuitBreakerPolicy(
        HttpRequestMessage message,
        BaseServiceOptions serviceOptions,
        ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .OrResult(r => r.StatusCode == (HttpStatusCode) 429) // Too Many Requests
            .CircuitBreakerAsync(
                serviceOptions.CircuitBreakerExceptionsCount,
                serviceOptions.CircuitBreakerDuration,
                (response, circuitState, timeSpan, _) =>
                {
                    logger.ErrorWithObject(null, "CB onBreak",new
                    {
                        message.RequestUri,
                        message.Method,
                        response.Result.StatusCode,
                        circuitState,
                        timeSpan
                    });
                }, 
                context =>
                {
                    logger.ErrorWithObject(null, "CB onReset",new
                    {
                        message.RequestUri,
                        message.Method,
                        context
                    });
                },
                () =>
                {
                    logger.ErrorWithObject(null, "CB onHalfOpen",new
                    {
                        message.RequestUri,
                        message.Method,
                    });
                });
    }
    
}