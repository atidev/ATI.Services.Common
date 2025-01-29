#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using NLog;

namespace ATI.Services.Common.Http;

internal static class HttpContextHelper
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    
    public static string[] MetricsHeadersValues(HttpContext? httpContext) => GetHeadersValues(httpContext, MetricsLabelsAndHeaders.UserHeaders);
    public static IReadOnlyDictionary<string, string> HeadersAndValuesToProxy(HttpContext? httpContext, IReadOnlyCollection<string>? headersToProxy) => GetHeadersAndValues(httpContext, headersToProxy);
    

    private static string[] GetHeadersValues(HttpContext? httpContext, IReadOnlyCollection<string>? headersNames)
    {
        try
        {
            if (headersNames is null || headersNames.Count == 0)
                return [];

            var headersValues = headersNames.Select(label => GetHeaderValue(httpContext, label)).ToArray();
            return headersValues;
        }
        catch (ObjectDisposedException ex) // when thing happen outside http ctx e.g eventbus event handler
        {
            Logger.ErrorWithObject(ex, headersNames);
            return [];
        }
    }

    private static string GetHeaderValue(HttpContext? context, string headerName)
    {
        if (context is null)
            return "This service";

        if (context.Request.Headers.TryGetValue(headerName, out var headerValues) && !StringValues.IsNullOrEmpty(headerValues))
            return headerValues[0]!;

        return "Empty";
    }

    private static IReadOnlyDictionary<string, string> GetHeadersAndValues(HttpContext? httpContext, IReadOnlyCollection<string>? headersNames)
    {
        if (headersNames is null || headersNames.Count == 0 || httpContext is null)
            return ReadOnlyDictionary<string, string>.Empty;

        return headersNames
            .Select(header => httpContext.Request.Headers.TryGetValue(header, out var headerValues)
                              && !StringValues.IsNullOrEmpty(headerValues)
                ? new
                {
                    Header = header,
                    Value = headerValues[0]
                }
                : null)
            .Where(headerAndValue => headerAndValue is not null)
            .ToDictionary(headerAndValue => headerAndValue!.Header,
                headerAndValue => headerAndValue!.Value)!;
    }
}