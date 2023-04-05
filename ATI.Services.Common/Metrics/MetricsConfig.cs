using JetBrains.Annotations;

namespace ATI.Services.Common.Metrics
{
    [UsedImplicitly]
    public class MetricsConfig
    {
        public static void Configure()
        {
            var exceptionCollector = new ExceptionsMetricsCollector();
            exceptionCollector.RegisterMetrics(Prometheus.Metrics.DefaultRegistry);
            
            Prometheus.Metrics.DefaultRegistry.AddBeforeCollectCallback(exceptionCollector.UpdateMetrics);
        }
    }
}
