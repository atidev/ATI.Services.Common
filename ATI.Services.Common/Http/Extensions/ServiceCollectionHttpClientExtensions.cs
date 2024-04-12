using System;
using System.Net.Http;
using ATI.Services.Common.Options;
using ATI.Services.Common.Variables;
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
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>s
    public static IServiceCollection AddCustomHttpClient<T>(this IServiceCollection services, Action<HttpClient> additionalActions) where T : BaseServiceOptions
    {
        var className = typeof(T).Name;
        var settings = ConfigurationManager.GetSection(className).Get<T>();
        var logger = LogManager.GetLogger(settings.ServiceName);

        var serviceVariablesOptions = ConfigurationManager.GetSection(nameof(ServiceVariablesOptions)).Get<ServiceVariablesOptions>();

        services.AddHttpClient(settings.ServiceName, httpClient =>
            {
                httpClient.SetBaseFields(serviceVariablesOptions.GetServiceAsClientName(), serviceVariablesOptions.GetServiceAsClientHeaderName(),  settings.AdditionalHeaders);
                additionalActions(httpClient);
            })
            .WithLogging<T>()
            .WithProxyFields<T>()
            .AddRetryPolicy(settings, logger)
            .AddHostSpecificCircuitBreakerPolicy(settings, logger)
            .AddTimeoutPolicy(settings.TimeOut)
            .WithMetrics<T>();

        return services;
    }
}