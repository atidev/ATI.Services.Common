using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Metrics;
using ATI.Services.Common.Options;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using Polly;
using Polly.CircuitBreaker;
using Polly.Contrib.WaitAndRetry;
using Polly.Extensions.Http;
using Polly.Registry;
using Polly.Timeout;
using Prometheus;

namespace ATI.Services.Common.Http.Extensions;

/// <summary>
/// https://habr.com/ru/companies/dododev/articles/503376/
/// </summary>
public static class HttpClientBuilderPolicyExtensions
{
    /// <summary>
    /// Func, because we need new iterator for every request (different time interval)
    /// </summary>
    private static readonly Func<TimeSpan, int, IEnumerable<TimeSpan>> RetryPolicyDelay =  (medianFirstRetryDelay, retryCount) => Backoff.DecorrelatedJitterBackoffV2(
        medianFirstRetryDelay: medianFirstRetryDelay,
        retryCount: retryCount);
    
    private static readonly Gauge _gauge = Prometheus.Metrics.CreateGauge($"{MetricsFactory.Prefix}_CircuitBreaker",
        string.Empty,
        new GaugeConfiguration
        {
            LabelNames = ["serviceName"]
        });

    public static IHttpClientBuilder AddRetryPolicy(
        this IHttpClientBuilder clientBuilder,
        BaseServiceOptions serviceOptions,
        ILogger logger)
    {
        var methodsToRetry = serviceOptions.HttpMethodsToRetry ?? new List<string> { HttpMethod.Get.Method };

        return clientBuilder
            .AddPolicyHandler((_, message) =>
            {
                var medianFirstRetryDelay = serviceOptions.MedianFirstRetryDelay;
                var retryCount = serviceOptions.RetryCount;
                var checkHttpMethod = true;

                var retryPolicySettings = message.Options.GetRetryPolicy();
                if (retryPolicySettings != null)
                {
                    if (retryPolicySettings.MedianFirstRetryDelay != null)
                        medianFirstRetryDelay = retryPolicySettings.MedianFirstRetryDelay.Value;

                    if (retryPolicySettings.RetryCount != null)
                        retryCount = retryPolicySettings.RetryCount.Value;

                    // If retrySettings set via HttpClient.SendAsync method - ignore http method check
                    checkHttpMethod = false;
                }

                if (retryCount == 0 || checkHttpMethod && !methodsToRetry.Contains(message.Method.Method, StringComparer.OrdinalIgnoreCase))
                {
                    return Policy.NoOpAsync<HttpResponseMessage>();
                }

                return HttpPolicyExtensions
                    .HandleTransientHttpError()
                    .Or<TimeoutRejectedException>()
                    .OrResult(r => r.StatusCode == (HttpStatusCode) 429) // Too Many Requests
                    .WaitAndRetryAsync(RetryPolicyDelay(medianFirstRetryDelay, retryCount),
                        (response, sleepDuration, retryCount, _) =>
                        {
                            logger.ErrorWithObject(response?.Exception, "Error while WaitAndRetry",  new
                            {
                                serviceOptions.ServiceName,
                                message.RequestUri,
                                message.Method,
                                response?.Result?.StatusCode,
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
            var circuitBreakerSamplingDuration = serviceOptions.CircuitBreakerSamplingDuration;
            var circuitBreakerDuration = serviceOptions.CircuitBreakerDuration;
            var circuitBreakerFailureThreshold = serviceOptions.CircuitBreakerFailureThreshold;
            var circuitBreakerMinimumThroughput = serviceOptions.CircuitBreakerMinimumThroughput;
            
            //подумать какое значение поставить в 0, чтобы не использовать CB
            if (circuitBreakerMinimumThroughput == 0)
                return Policy.NoOpAsync<HttpResponseMessage>();

            var policyKey = $"{message.RequestUri.Host}:{message.RequestUri.Port}";
            var policy = registry.GetOrAdd(policyKey,
                BuildCircuitBreakerPolicy(message, serviceOptions, circuitBreakerDuration,
                    circuitBreakerSamplingDuration, circuitBreakerFailureThreshold, circuitBreakerMinimumThroughput,
                    logger));
            return policy;
        });
    }

    public static IHttpClientBuilder AddTimeoutPolicy(
        this IHttpClientBuilder httpClientBuilder,
        TimeSpan timeout)
    {
        return httpClientBuilder.AddPolicyHandler((_, message) =>
        {
            var policyTimeout = message.Options.GetRetryPolicy()?.TimeOut ?? timeout;

            return Policy.TimeoutAsync<HttpResponseMessage>(policyTimeout);
        });
    }


    private static AsyncCircuitBreakerPolicy<HttpResponseMessage> BuildCircuitBreakerPolicy(
        HttpRequestMessage message,
        BaseServiceOptions serviceOptions,
        TimeSpan circuitBreakerDuration,
        TimeSpan circuitBreakerSamplingDuration,
        double circuitBreakerFailureThreshold,
        int circuitBreakerMinimumThroughput,
        ILogger logger)
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .Or<TimeoutRejectedException>()
            .OrResult(r => r.StatusCode == (HttpStatusCode) 429) // Too Many Requests
            .AdvancedCircuitBreakerAsync(
                failureThreshold: circuitBreakerFailureThreshold,
                samplingDuration: circuitBreakerSamplingDuration,
                minimumThroughput: circuitBreakerMinimumThroughput,
                durationOfBreak: circuitBreakerDuration,
                (response, circuitState, timeSpan, _) =>
                {
                    logger.ErrorWithObject(null, "CB onBreak", new
                    {
                        serviceOptions.ServiceName,
                        message.RequestUri,
                        message.Method,
                        response?.Result?.StatusCode,
                        circuitState,
                        timeSpan
                    });
                    _gauge.WithLabels(serviceOptions.ServiceName);
                    _gauge.Inc();
                },
                context =>
                {
                    logger.ErrorWithObject(null, "CB onReset", new
                    {
                        serviceOptions.ServiceName,
                        message.RequestUri,
                        message.Method,
                        context
                    });
                    _gauge.WithLabels(serviceOptions.ServiceName);
                    _gauge.Dec();
                },
                () =>
                {
                    logger.ErrorWithObject(null, "CB onHalfOpen", new
                    {
                        serviceOptions.ServiceName,
                        message.RequestUri,
                        message.Method,
                    });
                });
    }
}