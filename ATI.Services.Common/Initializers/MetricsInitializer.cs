using System.Threading.Tasks;
using ATI.Services.Common.Initializers.Interfaces;
using ATI.Services.Common.Metrics;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace ATI.Services.Common.Initializers
{
    [UsedImplicitly]
    [InitializeOrder(Order = InitializeOrder.First)]
    public class MetricsInitializer : IInitializer
    {
        private static bool _initialized;
        private readonly MetricsOptions _metricsOptions;

        public MetricsInitializer(IOptions<MetricsOptions> metricsOptions)
        {
            _metricsOptions = metricsOptions.Value;
        }

        public Task InitializeAsync()
        {
            if (_initialized)
            {
                return Task.CompletedTask;
            }
            
            MetricsFactory.Init(_metricsOptions.DefaultLongRequestTime);
            
            _initialized = true;
            return Task.CompletedTask;
        }

        public string InitStartConsoleMessage()
        {
            return "Start Metrics initializer";
        }

        public string InitEndConsoleMessage()
        {
            return $"End Metrics initializer, result {_initialized}";
        }
    }
}