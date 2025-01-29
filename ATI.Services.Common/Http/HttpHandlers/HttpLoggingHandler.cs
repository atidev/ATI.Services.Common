using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ATI.Services.Common.Http.Extensions;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Options;
using Microsoft.Extensions.Options;
using NLog;
using Polly.Timeout;

namespace ATI.Services.Common.Http.HttpHandlers;

public class HttpLoggingHandler<T> : HttpLoggingHandler where T : BaseServiceOptions
{
    public HttpLoggingHandler(IOptions<T> serviceOptions)
        : base( serviceOptions.Value)
    {
    }
}

public class HttpLoggingHandler : DelegatingHandler
{
    private readonly BaseServiceOptions _serviceOptions;
    private readonly ILogger _logger;

    protected HttpLoggingHandler(BaseServiceOptions serviceOptions)
    {
        _serviceOptions = serviceOptions;
        _logger = LogManager.GetLogger(serviceOptions.ServiceName);
        _logger.WarnWithObject("HttpLoggingHandler constructor", new { serviceOptions.ServiceName });
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        const string logMessage = "Unsuccess status code from service";

        try
        {
            var responseMessage = await base.SendAsync(request, ct);
            if (responseMessage.IsSuccessStatusCode)
                return responseMessage;

            var responseContent = await responseMessage.Content.ReadAsStringAsync(ct);

            // log every 5XX status code as error
            var logLevel = responseMessage.StatusCode >= HttpStatusCode.InternalServerError
                ? _serviceOptions.LogLevelOverride(LogLevel.Error)
                : _serviceOptions.LogLevelOverride(LogLevel.Warn);
            _logger.LogWithObject(logLevel, ex: null, logMessage, logObjects: new
            {
                _serviceOptions.ServiceName,
                request.Method,
                responseMessage.RequestMessage.RequestUri,
                responseMessage.StatusCode,
                Content = responseContent
            });

            return responseMessage;
        }
        catch (TimeoutRejectedException ex)
        {
            return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
        }
        catch (Exception ex)
        {
            _logger.LogWithObject(_serviceOptions.LogLevelOverride(LogLevel.Error),
                ex,
                logObjects: new
                {
                    MetricEntity = request.Options.GetMetricEntity(),
                    Method = request.Method,
                    Content = await request.Content.ReadAsStringAsync(ct),
                    Headers = request.Headers,
                    FullUri = request.RequestUri
                });

            // return 500 instead of exception to not provoke HttpClientExtensions methods catch block - otherwise we will get duplicate error logs
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }
    }
}