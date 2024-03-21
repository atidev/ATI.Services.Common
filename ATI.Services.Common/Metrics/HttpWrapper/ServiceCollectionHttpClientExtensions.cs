using System.Net.Http.Headers;
using ATI.Services.Common.Options;
using ATI.Services.Common.Variables;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using ConfigurationManager = ATI.Services.Common.Behaviors.ConfigurationManager;

namespace ATI.Services.Common.Metrics.HttpWrapper;

[PublicAPI]
public static class ServiceCollectionHttpClientExtensions
{
    public static IServiceCollection AddCustomHttpClient<T>(this IServiceCollection services) where T : BaseServiceOptions
    {
        var className = typeof(T).Name;
        var settings = ConfigurationManager.GetSection(className).Get<T>();
        var logger = LogManager.GetLogger(className);
        
        var serviceVariablesOptions = ConfigurationManager.GetSection(nameof(ServiceVariablesOptions)).Get<ServiceVariablesOptions>();
        var serviceAsClientName = serviceVariablesOptions.GetServiceAsClientName();
        var serviceAsClientHeaderName = serviceVariablesOptions.GetServiceAsClientHeaderName();

        services.AddHttpClient(settings.ConsulName, httpClient =>
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                if (!string.IsNullOrEmpty(serviceAsClientName)
                    && !string.IsNullOrEmpty(serviceAsClientHeaderName))
                {
                    httpClient.DefaultRequestHeaders.Add(
                        serviceAsClientHeaderName,
                        serviceAsClientName);
                }

                var additionalHeaders = settings.AdditionalHeaders;

                if (additionalHeaders is { Count: > 0 })
                {
                    foreach (var header in additionalHeaders)
                    {
                        httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                    }
                }
            })
            .AddRetryPolicy(settings, logger)
            .AddHostSpecificCircuitBreakerPolicy(settings, logger)
            .AddTimeoutPolicy(settings.TimeOut);

        return services;
    }
}