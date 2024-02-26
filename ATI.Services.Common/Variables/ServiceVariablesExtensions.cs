using ATI.Services.Common.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Common.Variables;

public static class ServiceVariablesExtensions
{
    [UsedImplicitly]
    public static void AddServiceVariables(this IServiceCollection services)
    {
            services.ConfigureByName<ServiceVariablesOptions>();
            services.AddTransient<ServiceVariablesInitializer>();
            
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            AppHttpContext.Services = services.BuildServiceProvider(new ServiceProviderOptions().ValidateOnBuild);
        }
}