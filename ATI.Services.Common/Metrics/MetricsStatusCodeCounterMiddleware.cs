using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Prometheus;

namespace ATI.Services.Common.Metrics
{
    public class MetricsStatusCodeCounterMiddleware
    {
        private static Counter counter = Prometheus.Metrics.CreateCounter("HttpStatusCodeCounter", "", new CounterConfiguration
        {
            LabelNames = new[] { "http_status_code", "client_name" }
        });

        private readonly RequestDelegate _next;
        public MetricsStatusCodeCounterMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);
            var clientName = AppHttpContext.GetClientName(context);
            counter.WithLabels(context.Response.StatusCode.ToString(), clientName).Inc();
        }
    }
}
