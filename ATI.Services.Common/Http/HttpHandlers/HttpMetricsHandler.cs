using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ATI.Services.Common.Http.Extensions;
using ATI.Services.Common.Metrics;
using ATI.Services.Common.Options;
using Microsoft.Extensions.Options;

namespace ATI.Services.Common.Http.HttpHandlers;

public class HttpMetricsHandler<T> : HttpMetricsHandler where T : BaseServiceOptions
{
    public HttpMetricsHandler(MetricsFactory metricsFactory, IOptions<T> serviceOptions) 
        : base(metricsFactory, serviceOptions.Value)
    {
    }
}

public class HttpMetricsHandler : DelegatingHandler
{
    private readonly MetricsInstance _metrics;

    protected HttpMetricsHandler(MetricsFactory metricsFactory, BaseServiceOptions serviceOptions)
    {
        _metrics = metricsFactory.CreateHttpClientMetricsFactory(serviceOptions.ServiceName,
            serviceOptions.ServiceName, serviceOptions.LongRequestTime);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var metricEntity = request.Options.GetMetricEntity();
        var urlTemplate = request.Options.GetUrlTemplate() ?? request.RequestUri?.PathAndQuery;

        using (_metrics.CreateLoggingMetricsTimer(metricEntity,
                   $"{request.Method.Method}:{urlTemplate}"))
        {
            try
            {
                return await base.SendAsync(request, ct);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}