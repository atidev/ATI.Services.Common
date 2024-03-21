#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ATI.Services.Common.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace ATI.Services.Common.Variables;

internal static class AppHttpContext
{
    public static string[] MetricsHeadersValues => GetHeadersValues(MetricsLabelsAndHeaders.UserHeaders);
    public static Dictionary<string, string> HeadersAndValuesToProxy(IReadOnlyCollection<string>? headersToProxy) => GetHeadersAndValues(headersToProxy);

    /// <summary>
    /// Provides static access to the current HttpContext
    /// </summary>
    private static HttpContext? Ctx
    {
        get
        {
            var serviceProvider = StaticServiceProvider.ServiceProvider;
            ArgumentNullException.ThrowIfNull(serviceProvider, "IServiceProvider can't be null. Try do services.AddServiceVariables() in app configuration");

            var httpContextAccessor = serviceProvider.GetService<IHttpContextAccessor>();
            return httpContextAccessor?.HttpContext;
        }
    }

    private static string[] GetHeadersValues(IReadOnlyCollection<string>? headersNames)
    {
        try
        {
            if (headersNames is null || headersNames.Count == 0)
                return Array.Empty<string>();

            var headersValues = headersNames.Select(label => GetHeaderValue(Ctx, label)).ToArray();
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

    private static Dictionary<string, string> GetHeadersAndValues(IReadOnlyCollection<string>? headersNames)
    {
        if (headersNames is null || headersNames.Count == 0 || Ctx is null)
            return new Dictionary<string, string>();

        return headersNames
            .Select(header => Ctx.Request.Headers.TryGetValue(header, out var headerValues)
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