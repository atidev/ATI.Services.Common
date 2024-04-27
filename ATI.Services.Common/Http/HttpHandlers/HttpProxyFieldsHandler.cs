using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ATI.Services.Common.Context;
using ATI.Services.Common.Localization;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using NLog;

namespace ATI.Services.Common.Http.HttpHandlers;

public class HttpProxyFieldsHandler<T> : HttpProxyFieldsHandler where T : BaseServiceOptions
{
    public HttpProxyFieldsHandler(IHttpContextAccessor httpContextAccessor, IOptions<T> serviceOptions)
        : base(serviceOptions.Value, httpContextAccessor)
    {
    }
}

public class HttpProxyFieldsHandler : DelegatingHandler
{
    private readonly BaseServiceOptions serviceOptions;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    protected HttpProxyFieldsHandler(BaseServiceOptions serviceOptions, IHttpContextAccessor httpContextAccessor)
    {
        this.serviceOptions = serviceOptions;
        _httpContextAccessor = httpContextAccessor;
        Logger.WarnWithObject("HttpProxyFieldsHandler constructor", new { serviceOptions.ServiceName });
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (serviceOptions.HeadersToProxy.Count != 0)
        {
            var headers = HttpContextHelper.HeadersAndValuesToProxy(_httpContextAccessor?.HttpContext, serviceOptions.HeadersToProxy);

            foreach (var header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }
        
        string acceptLanguage;
        if (serviceOptions.AddCultureToRequest
            && (acceptLanguage = FlowContext<RequestMetaData>.Current.AcceptLanguage) != null)
        {
            request.Headers.Add("Accept-Language", acceptLanguage);
        }

        return await base.SendAsync(request, ct);
    }
}