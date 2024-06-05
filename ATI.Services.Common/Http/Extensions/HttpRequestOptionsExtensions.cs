using System.Net.Http;
using ATI.Services.Common.Policies;

namespace ATI.Services.Common.Http.Extensions;

internal static class HttpRequestOptionsExtensions
{
    private static readonly HttpRequestOptionsKey<string> MetricEntityOptionsKey = new ("MetricEntity");
    private static readonly HttpRequestOptionsKey<string> UrlTemplateOptionsKey = new ("UrlTemplate");
    private static readonly HttpRequestOptionsKey<RetryPolicySettings> RetryPolicyOptionsKey = new ("RetryPolicy");

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
    
    public static void AddRetryPolicy(this HttpRequestOptions options, RetryPolicySettings settings)
    {
        options.Set(RetryPolicyOptionsKey, settings);
    }
    public static RetryPolicySettings GetRetryPolicy(this HttpRequestOptions options)
    {
        options.TryGetValue(RetryPolicyOptionsKey, out var retryPolicy);
        return retryPolicy;
    }
}