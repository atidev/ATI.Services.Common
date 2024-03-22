using System;
using System.Collections.Generic;
using System.Linq;
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

namespace ATI.Services.Common.Metrics.Http;

/// <summary>
/// https://habr.com/ru/companies/otus/articles/778574/
/// </summary>
public static class HttpClientBuilderPolicyExtensions
{
    /// <summary>
    /// Func, because we need new iterator for every request (different time interval)
    /// </summary>
    private static readonly Func<TimeSpan, int, IEnumerable<TimeSpan>> RetryPolicyDelay =  (medianFirstRetryDelay, retryCount) => Backoff.DecorrelatedJitterBackoffV2(
        medianFirstRetryDelay: medianFirstRetryDelay,
        retryCount: retryCount);

    public static IHttpClientBuilder AddRetryPolicy(
        this IHttpClientBuilder clientBuilder,
        BaseServiceOptions serviceOptions,
        ILogger logger)
    {
        return clientBuilder
            .AddPolicyHandler((_, message) =>
            {
                if (serviceOptions.HttpMethodsToRetry is { Count: > 0 })
                {
                    if (!serviceOptions.HttpMethodsToRetry.Contains(message.Method.Method, StringComparer.OrdinalIgnoreCase))
                        return Policy.NoOpAsync<HttpResponseMessage>();
                }
                // Retry only idempotent operations - GET
                else if (message.Method != HttpMethod.Get)
                    return Policy.NoOpAsync<HttpResponseMessage>();

                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<TimeoutRejectedException>()
                    .OrResult(r => r.StatusCode == (HttpStatusCode) 429) // Too Many Requests
                    .WaitAndRetryAsync(RetryPolicyDelay(serviceOptions.MedianFirstRetryDelay, serviceOptions.RetryCount),
                        (response, sleepDuration, retryCount, _) =>
                        {
                            logger.ErrorWithObject(response?.Exception, "Error while WaitAndRetry",  new
                            {
                                serviceOptions.ConsulName,
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
                    logger.ErrorWithObject(null, "CB onBreak", new
                    {
                        serviceOptions.ConsulName,
                        message.RequestUri,
                        message.Method,
                        response.Result.StatusCode,
                        circuitState,
                        timeSpan
                    });
                },
                context =>
                {
                    logger.ErrorWithObject(null, "CB onReset", new
                    {
                        serviceOptions.ConsulName,
                        message.RequestUri,
                        message.Method,
                        context
                    });
                },
                () =>
                {
                    logger.ErrorWithObject(null, "CB onHalfOpen", new
                    {
                        serviceOptions.ConsulName,
                        message.RequestUri,
                        message.Method,
                    });
                });
    }
}