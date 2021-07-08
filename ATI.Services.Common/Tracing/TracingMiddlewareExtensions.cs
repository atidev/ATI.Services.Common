using ATI.Services.Common.Extensions;
using ATI.Services.Common.ServiceVariables;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ATI.Services.Common.Tracing
{
    [PublicAPI]
    public static class TracingMiddlewareExtensions
    {
        public static void AddTracing(this IServiceCollection services)
        {
            services.ConfigureByName<TracingOptions>();
            services.AddSingleton<ZipkinManager>();
            services.AddTransient<TracingInitializer>();
            services.AddLogging();
            services.AddServiceVariables();
        }
        
        public static void UseTracing(this IApplicationBuilder app)
        {
            var lifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();
            var tracingInitializer = app.ApplicationServices.GetService<TracingInitializer>();

            lifetime.ApplicationStarted.Register(() => tracingInitializer.Start());
            lifetime.ApplicationStopped.Register(() => tracingInitializer.Stop());


            app.UseMiddleware<TracingMiddleware>();
        }
    }
}
