using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.Variables;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Prometheus;
using ConfigurationManager = ATI.Services.Common.Behaviors.ConfigurationManager;


namespace ATI.Services.Common.Metrics;

public class MetricsStatusCodeCounterMiddleware
{
    private readonly Counter _counter;

    private readonly RequestDelegate _next;

    public MetricsStatusCodeCounterMiddleware(RequestDelegate next)
    {
        var prefix = ConfigurationManager.GetSection(nameof(MetricsOptions))
                                         ?.Get<MetricsOptions>()?.MetricsServiceName is { } serviceName
                         ? $"common_{serviceName}"
                         : "common_default";

        _counter = Prometheus.Metrics.CreateCounter($"{prefix}_HttpStatusCodeCounter",
                                                    "",
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
        var param = new[] {context.Response.StatusCode.ToString()}.Concat(AppHttpContext.MetricsHeadersValues)
                                                                  .ToArray();
        _counter.WithLabels(param).Inc();
    }
}