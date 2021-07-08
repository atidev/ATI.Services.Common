using ATI.Services.Common.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Consul
{
    public static class ConsulExtensions
    {
        [PublicAPI]
        public static void AddConsul(this IServiceCollection services)
        {
            services.ConfigureByName<ConsulRegistratorOptions>();
            services.AddTransient<ConsulInitializer>();
        }
    }
}