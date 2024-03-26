#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ATI.Services.Common.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace ATI.Services.Common.Variables;

internal static class HttpContextHelper
{
    public static string[] MetricsHeadersValues(IHttpContextAccessor? httpContextAccessor) => GetHeadersValues(httpContextAccessor, MetricsLabelsAndHeaders.UserHeaders);
    public static Dictionary<string, string> HeadersAndValuesToProxy(IHttpContextAccessor? httpContextAccessor,IReadOnlyCollection<string>? headersToProxy) => GetHeadersAndValues(httpContextAccessor, headersToProxy);
    

    private static string[] GetHeadersValues(IHttpContextAccessor? HttpContextAccessor, IReadOnlyCollection<string>? headersNames)
    {
        try
        {
            if (headersNames is null || headersNames.Count == 0)
                return Array.Empty<string>();

            var headersValues = headersNames.Select(label => GetHeaderValue(HttpContextAccessor?.HttpContext, label)).ToArray();
            return headersValues;
        }
        catch (ObjectDisposedException) // when thing happen outside http ctx e.g eventbus event handler
        {
            return Array.Empty<string>();
        }
    }

    private static string GetHeaderValue(HttpContext? context, string headerName)
    {
        if (context is null)
            return "This service";

        if (context.Request.Headers.TryGetValue(headerName, out var headerValues) && !StringValues.IsNullOrEmpty(headerValues))
            return headerValues[0];

        return "Empty";
    }

    private static Dictionary<string, string> GetHeadersAndValues(IHttpContextAccessor? httpContextAccessor, IReadOnlyCollection<string>? headersNames)
    {
        if (headersNames is null || headersNames.Count == 0 || httpContextAccessor?.HttpContext is null)
            return new Dictionary<string, string>();

        return headersNames
            .Select(header => httpContextAccessor.HttpContext.Request.Headers.TryGetValue(header, out var headerValues)
                              && !StringValues.IsNullOrEmpty(headerValues)
                ? new
                {
                    Header = header,
                    Value = headerValues[0]
                }
                : null)
            .Where(headerAndValue => headerAndValue != null)
            .ToDictionary(headerAndValue => headerAndValue!.Header,
                headerAndValue => headerAndValue!.Value);
    }
}