using System;
using System.Collections.Generic;
using System.Linq;
using ATI.Services.Common.Extensions;
using ATI.Services.Common.Metrics;
using Microsoft.AspNetCore.Http;

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
                    throw new Exception("Can't set once a value has already been set.");
                }

                _services = value;
            }
        }

        public static string[] MetricsHeadersValues => GetHeadersValues(Current, MetricsLabelsAndHeaders.UserHeaders);
        public static Dictionary<string, string> GetHeadersAndValuesToProxy => GetHeadersAndValues(Current, ServiceVariables.HeadersToProxy);

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

        private static string[] GetHeadersValues(HttpContext context, IEnumerable<string> headersNames)
        {
            var headersValues = headersNames.Select(label => GetHeaderValue(context, label)).ToArray();
            return headersValues;
        }

        private static Dictionary<string, string> GetHeadersAndValues(HttpContext context, IEnumerable<string> headersNames)
        {
            return headersNames
                .Select(header => context.Request.Headers.TryGetValue(header, out var headerValues)
                                  && !string.IsNullOrEmpty(headerValues)
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