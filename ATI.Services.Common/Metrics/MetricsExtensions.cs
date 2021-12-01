using ATI.Services.Common.Extensions;
using ATI.Services.Common.Initializers;
using ATI.Services.Common.Tracing;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Common.Metrics
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public static class MetricsExtensions
    {
        public static void AddMetrics(this IServiceCollection services)
        {
            services.ConfigureByName<TracingOptions>();
            services.AddSingleton<ZipkinManager>();
            services.AddTransient<MetricsInitializer>();
            MetricsConfig.Configure();
        }

        public static void UseMetrics(this IApplicationBuilder app)
        {
            app.UseMiddleware<MetricsStatusCodeCounterMiddleware>();
        }
    }
}