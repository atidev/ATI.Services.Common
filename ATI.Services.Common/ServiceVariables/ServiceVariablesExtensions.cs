using ATI.Services.Common.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Common.ServiceVariables
{
    public static class ServiceVariablesExtensions
    {
        [UsedImplicitly]
        public static void AddServiceVariables(this IServiceCollection services)
        {
            services.ConfigureByName<ServiceVariablesOptions>();
            services.AddTransient<ServiceVariablesInitializer>();
        }
    }
}
