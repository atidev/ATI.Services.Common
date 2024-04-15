using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using ATI.Services.Common.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using ConfigurationManager = ATI.Services.Common.Behaviors.ConfigurationManager;

namespace ATI.Services.Common.Http.Extensions;

[PublicAPI]
public static class ServiceCollectionHttpClientExtensions
{
    /// <summary>
    /// Add HttpClient to HttpClientFactory with retry/cb/timeout policy, logging, metrics
    /// Use it for external requests, like requests to integrators
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>s
    public static IServiceCollection AddCustomHttpClient<T>(this IServiceCollection services, Action<HttpClient> additionalActions) where T : BaseServiceOptions
    {
        var className = typeof(T).Name;
        var settings = ConfigurationManager.GetSection(className).Get<T>();
        var logger = LogManager.GetLogger(settings.ServiceName);

        services.AddHttpClient(settings.ServiceName, httpClient =>
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (settings.AdditionalHeaders is { Count: > 0 })
                {
                    foreach (var header in settings.AdditionalHeaders)
                    {
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }

                additionalActions(httpClient);
            })
            .WithLogging<T>()
            .WithProxyFields<T>()
            .AddRetryPolicy(settings, logger)
            .AddHostSpecificCircuitBreakerPolicy(settings, logger)
            .AddTimeoutPolicy(settings.TimeOut)
            .WithMetrics<T>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                ActivityHeadersPropagator = DistributedContextPropagator.CreateNoOutputPropagator()
            });

        return services;
    }
}