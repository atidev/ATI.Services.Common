using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.Initializers;
using ATI.Services.Common.Initializers.Interfaces;
using Microsoft.Extensions.Options;

namespace ATI.Services.Common.Metrics
{
    [InitializeOrder(Order = InitializeOrder.First)]
    public class MetricsOptionsInitializer : IInitializer
    {
        private static bool _initialized;
        private readonly MetricsOptions _options;

        public MetricsOptionsInitializer(IOptions<MetricsOptions> options)
        {
            _options = options.Value;
        }

        public Task InitializeAsync()
        {
            if (_initialized)
            {
                return Task.CompletedTask;
            }

            MetricsLabels.LabelsStatic = _options?.LabelsAndHeaders ?? new Dictionary<string, string>();
            MetricsLabels.UserLabels = MetricsLabels.LabelsStatic?.Keys?.ToArray();
            MetricsLabels.UserHeaders = MetricsLabels.LabelsStatic?.Values?.ToArray();

            _initialized = true;
            return Task.CompletedTask;
        }
    }
}