using System;
using ATI.Services.Common.Behaviors;
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

        public static string ClientName
        {
            get
            {
                if (Current == null)
                {
                    return null;
                }
                if (Current.Items.TryGetValue(CommonBehavior.ClientNameItemKey, out var clientNameValue))
                {
                    return clientNameValue as string;
                }

                return null;
            }
        }
    }
}