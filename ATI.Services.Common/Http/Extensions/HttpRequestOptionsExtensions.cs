using System.Net.Http;

namespace ATI.Services.Common.Http.Extensions;

internal static class HttpRequestOptionsExtensions
{
    private static readonly HttpRequestOptionsKey<string> MetricEntityOptionsKey = new ("MetricEntity");
    private static readonly HttpRequestOptionsKey<string> UrlTemplateOptionsKey = new ("UrlTemplate");

    public static void AddMetricEntity(this HttpRequestOptions options, string metricEntity)
    {
        options.Set(MetricEntityOptionsKey, metricEntity);
    }
    public static string GetMetricEntity(this HttpRequestOptions options)
    {
        options.TryGetValue(MetricEntityOptionsKey, out var metricEntity);
        return metricEntity ?? "None";
    }
    
    public static void AddUrlTemplate(this HttpRequestOptions options, string urlTemplate)
    {
        options.Set(UrlTemplateOptionsKey, urlTemplate);
    }
    public static string GetUrlTemplate(this HttpRequestOptions options)
    {
        options.TryGetValue(UrlTemplateOptionsKey, out var urlTemplate);
        return urlTemplate;
    }
}