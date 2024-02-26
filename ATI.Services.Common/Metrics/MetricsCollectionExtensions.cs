using System.Net;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Prometheus;

namespace ATI.Services.Common.Metrics;

[PublicAPI]
public static class MetricsCollectionExtensions
{
    private const string MetricsCollectionAddress = "metrics";

    public static IEndpointConventionBuilder MapMetricsCollection(this IEndpointRouteBuilder builder)
    {
        return builder.MapGet(MetricsCollectionAddress, MetricsCollectionDelegate);
    }

    private static async Task MetricsCollectionDelegate(HttpContext httpContext)
    {
        await GetMetrics(httpContext);
    }
        
    private static async Task GetMetrics(HttpContext httpContext)
    {
        var response = httpContext.Response;

        response.ContentType = PrometheusConstants.TextContentType;
        response.StatusCode = (int)HttpStatusCode.OK;

        await using var outputStream = response.Body;
        await Prometheus.Metrics.DefaultRegistry.CollectAndExportAsTextAsync(outputStream);
    }
}