using System;
using System.Linq;
using System.Net.Http.Headers;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Options;
using ATI.Services.Common.Variables;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NLog;
using ConfigurationManager = ATI.Services.Common.Behaviors.ConfigurationManager;

namespace ATI.Services.Common.Metrics.Http;

[PublicAPI]
public static class ServiceCollectionHttpClientExtensions
{
    /// <summary>
    /// Dynamically add all inheritors of BaseServiceOptions as AddCustomHttpClient<T>
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddCustomHttpClients(this IServiceCollection services)
    {
        var servicesOptionsTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsSubclassOf(typeof(BaseServiceOptions)));

        foreach (var serviceOptionType in servicesOptionsTypes)
        {
            var method = typeof(ServiceCollectionHttpClientExtensions)
                .GetMethod(nameof(AddCustomHttpClient), new[] { typeof(IServiceCollection) });
            var generic = method.MakeGenericMethod(serviceOptionType);
            generic.Invoke(null, new[] { services });
        }

        return services;
    }
    
    /// <summary>
    /// Add HttpClient to HttpClientFactory with retry/cb/timeout policy 
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>s
    public static IServiceCollection AddCustomHttpClient<T>(this IServiceCollection services) where T : BaseServiceOptions
    {
        var className = typeof(T).Name;
        var settings = ConfigurationManager.GetSection(className).Get<T>();
        var logger = LogManager.GetLogger(className);
        
        if (!settings.UseHttpClientFactory)
        {
            logger.WarnWithObject($"Class ${className} has UseHttpClientFactory == false while AddCustomHttpClient");
            return services;
        }

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