using System.Collections.Generic;
using JetBrains.Annotations;
using Prometheus.Advanced;

namespace ATI.Services.Common.Metrics
{
    [UsedImplicitly]
    public class MetricsConfig
    {
        public static void Configure()
        {
            DefaultCollectorRegistry.Instance.Clear();

            var customCollectors = new List<IOnDemandCollector>
            {
                new SystemMetricsCollector(),
                new ExceptionsMetricsCollector()
            };

            DefaultCollectorRegistry.Instance.RegisterOnDemandCollectors(customCollectors);
        }
    }
}
