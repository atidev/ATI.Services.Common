using System;
using System.Collections.Generic;
using System.Linq;
using ATI.Services.Common.Extensions;
using ATI.Services.Common.Metrics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace ATI.Services.Common.ServiceVariables
{
    internal static class AppHttpContext
    {
        private static IServiceProvider _services;

        /// <summary>
        /// Provides static access to the framework's services provider
        /// </summary>
        public static IServiceProvider Services
        {
            get => _services;
            set
            {
                if (_services != null)
                {
                    return;
                }

                _services = value;
            }
        }

        public static string[] MetricsHeadersValues => GetHeadersValues(MetricsLabelsAndHeaders.UserHeaders);
        public static Dictionary<string, string> HeadersAndValuesToProxy => GetHeadersAndValues(ServiceVariables.HeadersToProxy);

        /// <summary>
        /// Provides static access to the current HttpContext
        /// </summary>
        private static HttpContext Current
        {
            get
            {
                var httpContextAccessor =
                    _services.GetService(typeof(IHttpContextAccessor)) as IHttpContextAccessor;
                return httpContextAccessor?.HttpContext;
            }
        }

        private static string[] GetHeadersValues(string[] headersNames)
        {
            if (headersNames == null || headersNames.Length == 0)
                return headersNames;

            var context = Current;
            var headersValues = headersNames.Select(label => GetHeaderValue(context, label)).ToArray();
            return headersValues;
        }

        private static string GetHeaderValue(HttpContext context, string headerName)
        {
            if (context == null)
            {
                return "This service";
            }

            if (context.Request.Headers.TryGetValue(headerName, out var headerValues))
            {
                if (!headerValues[0].IsNullOrEmpty())
                {
                    return headerValues[0];
                }
            }

            return "Empty";
        }

        private static Dictionary<string, string> GetHeadersAndValues(IReadOnlyCollection<string> headersNames)
        {
            if (headersNames == null || headersNames.Count == 0)
                return null;

            var context = Current;
            if (context == null)
                return null;

            return headersNames
                .Select(header => context.Request.Headers.TryGetValue(header, out var headerValues)
                                  && !StringValues.IsNullOrEmpty(headerValues)
                    ? new
                    {
                        Header = header,
                        Value = headerValues[0]
                    }
                    : null)
                .Where(headerAndValue => headerAndValue != null)
                .ToDictionary(headerAndValue => headerAndValue.Header,
                    headerAndValue => headerAndValue.Value);
        }
    }
}