using ATI.Services.Common.Http.HttpHandlers;
using ATI.Services.Common.Options;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Common.Http.Extensions;

[PublicAPI]
public static class HttpClientBuilderExtensions
{
    public static IHttpClientBuilder WithProxyFields<TServiceOptions>(this IHttpClientBuilder httpClientBuilder)
        where TServiceOptions : BaseServiceOptions
    {
        httpClientBuilder.Services.AddTransient<HttpProxyFieldsHandler<TServiceOptions>>();

        return httpClientBuilder.AddHttpMessageHandler<HttpProxyFieldsHandler<TServiceOptions>>();
    }
    
    public static IHttpClientBuilder WithLogging<TServiceOptions>(this IHttpClientBuilder httpClientBuilder)
        where TServiceOptions : BaseServiceOptions
    {
        httpClientBuilder.Services.AddTransient<HttpLoggingHandler<TServiceOptions>>();

        return httpClientBuilder.AddHttpMessageHandler<HttpLoggingHandler<TServiceOptions>>();
    }
    
    public static IHttpClientBuilder WithMetrics<TServiceOptions>(this IHttpClientBuilder httpClientBuilder)
        where TServiceOptions : BaseServiceOptions
    {
        httpClientBuilder.Services.AddTransient<HttpMetricsHandler<TServiceOptions>>();

        return httpClientBuilder
            .AddHttpMessageHandler<HttpMetricsHandler<TServiceOptions>>();
    }
}