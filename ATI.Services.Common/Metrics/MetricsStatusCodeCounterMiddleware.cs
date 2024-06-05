using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.Http;
using ATI.Services.Common.Variables;
using Microsoft.AspNetCore.Http;
using Prometheus;

namespace ATI.Services.Common.Metrics;

public class MetricsStatusCodeCounterMiddleware
{
    private readonly Counter _counter;
    private readonly RequestDelegate _next;

    public MetricsStatusCodeCounterMiddleware(RequestDelegate next)
    {
        _counter = Prometheus.Metrics.CreateCounter($"{MetricsFactory.Prefix}_HttpStatusCodeCounter",
            string.Empty,
                                                    new CounterConfiguration
                                                    {
                                                        LabelNames = new[] { "http_status_code" }
                                                                     .Concat(MetricsLabelsAndHeaders.UserLabels)
                                                                     .ToArray()
                                                    });
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
        var param = new[] {context.Response.StatusCode.ToString()}.Concat(HttpContextHelper.MetricsHeadersValues(context))
                                                                  .ToArray();
        _counter.WithLabels(param).Inc();
    }
}