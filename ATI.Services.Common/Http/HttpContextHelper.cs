#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ATI.Services.Common.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace ATI.Services.Common.Http;

internal static class HttpContextHelper
{
    public static string[] MetricsHeadersValues(HttpContext? httpContext) => GetHeadersValues(httpContext, MetricsLabelsAndHeaders.UserHeaders);
    public static Dictionary<string, string> HeadersAndValuesToProxy(HttpContext? httpContext,IReadOnlyCollection<string>? headersToProxy) => GetHeadersAndValues(httpContext, headersToProxy);
    

    private static string[] GetHeadersValues(HttpContext? httpContext, IReadOnlyCollection<string>? headersNames)
    {
        try
        {
            if (headersNames is null || headersNames.Count == 0)
                return Array.Empty<string>();

            var headersValues = headersNames.Select(label => GetHeaderValue(httpContext, label)).ToArray();
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

    private static Dictionary<string, string> GetHeadersAndValues(HttpContext? httpContext, IReadOnlyCollection<string>? headersNames)
    {
        if (headersNames is null || headersNames.Count == 0 || httpContext is null)
            return new Dictionary<string, string>();

        return headersNames
            .Select(header => httpContext.Request.Headers.TryGetValue(header, out var headerValues)
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