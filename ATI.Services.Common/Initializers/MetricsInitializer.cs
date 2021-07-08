using System.Threading.Tasks;
using ATI.Services.Common.Initializers.Interfaces;
using ATI.Services.Common.Metrics;
using ATI.Services.Common.Tracing;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace ATI.Services.Common.Initializers
{
    [UsedImplicitly]
    [InitializeOrder(Order = InitializeOrder.Second)]
    public class MetricsInitializer : IInitializer
    {
        private static bool _initialized;
        private readonly ZipkinManager _zipkinManager;
        private readonly TracingOptions _tracingOptions;

        public MetricsInitializer(ZipkinManager zipkinManager, IOptions<TracingOptions> tracingOptions)
        {
            _zipkinManager = zipkinManager;
            _tracingOptions = tracingOptions.Value;
        }

        public Task InitializeAsync()
        {
            if (_initialized)
            {
                return Task.CompletedTask;
            }
            
            _zipkinManager.Init(_tracingOptions);
            MeasureAttribute.Initialize(_zipkinManager);
            
            if (_tracingOptions.MetricsServiceName != null )
            {
                MetricsTracingFactory.Init(_tracingOptions.MetricsServiceName, _tracingOptions.DefaultLongRequestTime);
            }
            
            _initialized = true;
            return Task.CompletedTask;
        }
    }
}