using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Extensions;
using ATI.Services.Common.Initializers;
using ATI.Services.Common.Tracing;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            AppHttpContext.Services = services.BuildServiceProvider(new ServiceProviderOptions().ValidateOnBuild);

            MetricsLabelsAndHeaders.LabelsStatic = ConfigurationManager.GetSection(nameof(MetricsOptions))?.Get<MetricsOptions>()?.LabelsAndHeaders ?? new Dictionary<string, string>();
            MetricsLabelsAndHeaders.UserLabels = MetricsLabelsAndHeaders.LabelsStatic.Keys.ToArray();
            MetricsLabelsAndHeaders.UserHeaders = MetricsLabelsAndHeaders.LabelsStatic.Values.ToArray();
        }

        public static void UseMetrics(this IApplicationBuilder app)
        {
            app.UseMiddleware<MetricsStatusCodeCounterMiddleware>();
        }
    }
}