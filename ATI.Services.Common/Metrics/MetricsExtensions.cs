using System.Collections.Generic;
using System.Linq;
using ATI.Services.Common.Extensions;
using ATI.Services.Common.Initializers;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ConfigurationManager = ATI.Services.Common.Behaviors.ConfigurationManager;

namespace ATI.Services.Common.Metrics;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class MetricsExtensions
{
    /// <summary>
    /// Alias to distinguish from Microsoft.Extensions.DependencyInjection.MetricsServiceExtensions.AddMetrics(IServiceCollection) 
    /// </summary>
    public static void AddCommonMetrics(this IServiceCollection services) => services.AddMetrics();

    public static void AddMetrics(this IServiceCollection services)
    {
        services.ConfigureByName<MetricsOptions>();
        services.AddTransient<MetricsInitializer>();
            
        InitializeExceptionsMetrics();

        MetricsLabelsAndHeaders.LabelsStatic = ConfigurationManager.GetSection(nameof(MetricsOptions))?.Get<MetricsOptions>()?.LabelsAndHeaders ?? new Dictionary<string, string>();
        MetricsLabelsAndHeaders.UserLabels = MetricsLabelsAndHeaders.LabelsStatic.Keys.ToArray();
        MetricsLabelsAndHeaders.UserHeaders = MetricsLabelsAndHeaders.LabelsStatic.Values.ToArray();
    }

    private static void InitializeExceptionsMetrics()
    {
        var exceptionCollector = new ExceptionsMetricsCollector();
        var registry = Prometheus.Metrics.DefaultRegistry;
            
        exceptionCollector.RegisterMetrics(registry);
        registry.AddBeforeCollectCallback(exceptionCollector.UpdateMetrics);
    }

    public static void UseMetrics(this IApplicationBuilder app)
    {
        app.UseMiddleware<MetricsStatusCodeCounterMiddleware>();
    }
}