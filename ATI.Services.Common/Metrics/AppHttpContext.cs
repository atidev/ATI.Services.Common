using System;
using System.Collections.Generic;
using System.Linq;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Extensions;
using ATI.Services.Common.Tracing;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;


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
        
        public static string[] HeadersValues => GetHeadersValues(Current, MetricsOptions.UserHeaders);


        private static string GetHeaderValue(HttpContext context, string headerName)
        {
            if (context == null)
            {
                return ServiceVariables.ServiceVariables.ServiceAsClientName;
            }

            context.Request.Headers.TryGetValue(headerName, out var headerValueObject);
            var headerValue = headerValueObject[0];
            if (!headerValue.IsNullOrEmpty())
            {
                return headerValue;
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