using System.Net;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Prometheus;
using Prometheus.Advanced;

namespace ATI.Services.Common.Metrics
{
    [PublicAPI]
    public static class MetricsCollectionExtensions
    {
        private const string MetricsCollectionAddress = "metrics";

        public static IEndpointConventionBuilder MapMetricsCollection(this IEndpointRouteBuilder builder)
        {
            return builder.MapGet(MetricsCollectionAddress, MetricsCollectionDelegate);
        }

        private static Task MetricsCollectionDelegate(HttpContext httpContext)
        {
            GetMetrics(httpContext);
            return Task.CompletedTask;
        }
        
        private static void GetMetrics(HttpContext httpContext)
        {
            var syncIoFeature = httpContext.Features.Get<IHttpBodyControlFeature>();

            if (syncIoFeature != null)
            {
                syncIoFeature.AllowSynchronousIO = true;
            }

            var request = httpContext.Request;
            var response = httpContext.Response;

            var acceptHeaders = request.Headers["Accept"];

            response.ContentType = ScrapeHandler.GetContentType(acceptHeaders);
            response.StatusCode = (int)HttpStatusCode.OK;

            using var outputStream = response.Body;
            var collected = DefaultCollectorRegistry.Instance.CollectAll();
            ScrapeHandler.ProcessScrapeRequest(collected, response.ContentType, outputStream);
        }
    }
}