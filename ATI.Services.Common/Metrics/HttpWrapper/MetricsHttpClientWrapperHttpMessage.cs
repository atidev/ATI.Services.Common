using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using ATI.Services.Common.Context;
using ATI.Services.Common.Extensions;
using ATI.Services.Common.Http;
using ATI.Services.Common.Localization;
using ATI.Services.Common.Variables;
using Microsoft.AspNetCore.Http;

namespace ATI.Services.Common.Metrics.HttpWrapper;

internal class MetricsHttpClientWrapperHttpMessage
{
    private const string ContentTypeHeaderName = "Content-Type";

    public MetricsHttpClientWrapperHttpMessage(HttpMethod method, Uri fullUri, Dictionary<string, string> headers)
    {
        Method = method;
        FullUri = fullUri;
        Headers = new Dictionary<string, string>();
        ContentType = "application/json";

        if (headers == null)
            return;

        foreach (var header in headers)
        {
            if (string.Equals(header.Key, ContentTypeHeaderName,
                    StringComparison.InvariantCultureIgnoreCase))
            {
                ContentType = header.Value;
            }
            else
            {
                Headers.Add(header.Key, header.Value);
            }
        }
    }

    public HttpMethod Method { get; }
    public string Content { get; init; }
    public Uri FullUri { get; }
    public Dictionary<string, string> Headers { get; }
    private string ContentType { get; }

    internal HttpRequestMessage ToRequestMessage(MetricsHttpClientConfig config, IHttpContextAccessor httpContextAccessor)
    {
        var msg = new HttpRequestMessage(Method, FullUri);

        if (config.HeadersToProxy.Count != 0)
            Headers.AddRange(HttpContextHelper.HeadersAndValuesToProxy(httpContextAccessor?.HttpContext, config.HeadersToProxy).ToDictionary());

        foreach (var header in Headers)
            msg.Headers.TryAddWithoutValidation(header.Key, header.Value);

        string acceptLanguage;
        if (config.AddCultureToRequest
            && (acceptLanguage = FlowContext<RequestMetaData>.Current.AcceptLanguage) != null)
            msg.Headers.Add("Accept-Language", acceptLanguage);


        if (string.IsNullOrEmpty(Content) == false)
        {
            msg.Content = new StringContent(Content, Encoding.UTF8, ContentType);
        }

        return msg;
    }
}