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
    private readonly ILogger Logger;

    protected HttpLoggingHandler(BaseServiceOptions serviceOptions)
    {
        _serviceOptions = serviceOptions;
        Logger = LogManager.GetLogger(serviceOptions.ServiceName);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        const string LogMessageTemplate =
            "Сервис:{0} в ответ на запрос [HTTP {1} {2}] вернул ответ с статус кодом {3}.";

        try
        {
            var responseMessage = await base.SendAsync(request, ct);
            if (responseMessage.IsSuccessStatusCode)
                return responseMessage;

            var logMessage = string.Format(LogMessageTemplate, _serviceOptions.ServiceName, request.Method,
                responseMessage.RequestMessage.RequestUri, responseMessage.StatusCode);
            var responseContent = await responseMessage.Content.ReadAsStringAsync(ct);

            var logLevel = responseMessage.StatusCode == HttpStatusCode.InternalServerError
                ? _serviceOptions.LogLevelOverride(LogLevel.Error)
                : _serviceOptions.LogLevelOverride(LogLevel.Warn);
            Logger.LogWithObject(logLevel, ex: null, logMessage, logObjects: responseContent);

            return responseMessage;
        }
        catch (Exception ex)
        {
            Logger.LogWithObject(_serviceOptions.LogLevelOverride(LogLevel.Error),
                ex,
                logObjects: new
                {
                    MetricEntity = request.Options.GetMetricEntity(),
                    Method = request.Method,
                    Content = await request.Content.ReadAsStringAsync(ct),
                    Headers = request.Headers,
                    FullUri = request.RequestUri
                });

            // return 500 to not provoke HttpClientExtensions methods catch block - otherwise we will get duplicate error logs
            return new HttpResponseMessage(HttpStatusCode.InternalServerError);
        }
    }
}