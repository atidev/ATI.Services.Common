#nullable enable
using System;
using System.Diagnostics;
using System.Net.Http;
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
    /// Add named HttpClient as TServiceOptions.ServiceName to HttpClientFactory with retry/cb/timeout policy, logging, metrics
    /// Use it for external requests, like requests to integrators
    /// https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory#named-clients
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TServiceOptions"></typeparam>
    /// <returns></returns>s
    public static IServiceCollection AddCustomHttpClient<TServiceOptions>(this IServiceCollection services,
        Action<HttpClient>? additionalActions = null)
        where TServiceOptions : BaseServiceOptions
    {
        var (settings, logger) = GetInitialData<TServiceOptions>();

        services.AddHttpClient(settings.ServiceName, httpClient =>
            {
                ConfigureHttpClientHeaders(httpClient, settings);
                additionalActions?.Invoke(httpClient);
            })
            .AddDefaultHandlers(settings, logger);

        return services;
    }

    /// <summary>
    /// Add typed HttpClient to HttpClientFactory with retry/cb/timeout policy, logging, metrics
    /// Use it for external requests, like requests to integrators
    /// https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory#typed-clients
    /// </summary>
    /// <param name="services"></param>
    /// <typeparam name="TAdapter">Type of the http adapter for typed HttpClient</typeparam>
    /// <typeparam name="TServiceOptions"></typeparam>
    /// <returns></returns>s
    public static IServiceCollection AddCustomHttpClient<TAdapter, TServiceOptions>(this IServiceCollection services,
        Action<HttpClient>? additionalActions = null)
        where TAdapter : class
        where TServiceOptions : BaseServiceOptions
    {
        var (settings, logger) = GetInitialData<TServiceOptions>();

        services.AddHttpClient<TAdapter>(httpClient =>
            {
                ConfigureHttpClientHeaders(httpClient, settings);
                additionalActions?.Invoke(httpClient);
            })
            .AddDefaultHandlers(settings, logger);

        return services;
    }


    private static (TServiceOptions settings, ILogger? logger) GetInitialData<TServiceOptions>()
        where TServiceOptions : BaseServiceOptions
    {
        var className = typeof(TServiceOptions).Name;
        var settings = ConfigurationManager.GetSection(className).Get<TServiceOptions>();

        if (settings is null)
            throw new NullReferenceException($"Please configure {nameof(TServiceOptions)} options");

        var logger = LogManager.GetLogger(settings.ServiceName);
        return (settings, logger);
    }

    private static void ConfigureHttpClientHeaders(HttpClient httpClient, BaseServiceOptions settings)
    {
        if (settings.AdditionalHeaders is null || settings.AdditionalHeaders.Count == 0) return;

        foreach (var header in settings.AdditionalHeaders)
        {
            httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }
    }

    private static IHttpClientBuilder AddDefaultHandlers<TServiceOptions>(this IHttpClientBuilder builder,
        TServiceOptions settings, ILogger? logger)
        where TServiceOptions : BaseServiceOptions
    {
        return builder
            .WithLogging<TServiceOptions>()
            .WithProxyFields<TServiceOptions>()
            .AddRetryPolicy(settings, logger)
            .AddHostSpecificCircuitBreakerPolicy(settings, logger)
            .AddTimeoutPolicy(settings.TimeOut)
            .WithMetrics<TServiceOptions>()
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                PooledConnectionLifetime = settings.PooledConnectionLifetime,
                // Disable extra information for external requests
                ActivityHeadersPropagator = DistributedContextPropagator.CreateNoOutputPropagator()
            });
    }
}