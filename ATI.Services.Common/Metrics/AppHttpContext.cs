using System;
using System.Collections.Generic;
using System.Linq;
using ATI.Services.Common.Extensions;
using Microsoft.AspNetCore.Http;


namespace ATI.Services.Common.Metrics
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

        public static string[] HeadersValues => GetHeadersValues(Current, MetricsLabels.UserLabels);


        private static string GetHeaderValue(HttpContext context, string labelName)
        {
            if (context == null)
            {
                return "This service";
            }

            if (context.Request.Headers.TryGetValue(labelName, out var headerValues))
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
            var labels = headersNames.Select(label => GetHeaderValue(context, label)).ToArray();
            return labels;
        }
    }
}