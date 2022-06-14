using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.ServiceVariables;
using Microsoft.AspNetCore.Http;
using Prometheus;

namespace ATI.Services.Common.Metrics
{
    public class MetricsStatusCodeCounterMiddleware
    {
        private static Counter counter = Prometheus.Metrics.CreateCounter("HttpStatusCodeCounter", "",
            new CounterConfiguration
            {
                LabelNames = new[] {"http_status_code"}.Concat(MetricsLabelsAndHeaders.UserLabels).ToArray()
            });

        private readonly RequestDelegate _next;

        public MetricsStatusCodeCounterMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next(context);
            var param = new[] {context.Response.StatusCode.ToString()}.Concat(AppHttpContext.MetricsHeadersValues)
                .ToArray();
            counter.WithLabels(param).Inc();
        }
    }
}