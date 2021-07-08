using System;
using JetBrains.Annotations;

namespace ATI.Services.Common.Metrics
{
    [PublicAPI]
    public class TimersWrapper : IDisposable
    {
        private readonly MetricsTimer _metricsTimer;
        private readonly TracingTimer _tracingTimer;

        public TimersWrapper(TracingTimer tracingTimer)
        {
            _tracingTimer = tracingTimer;
        }

        public TimersWrapper(MetricsTimer metricsTimer)
        {
            _metricsTimer = metricsTimer;
        }
        public TimersWrapper(MetricsTimer metricsTimer, TracingTimer tracingTimer)
        {
            _metricsTimer = metricsTimer;
            _tracingTimer = tracingTimer;
        }

        public void Restart()
        {
            _metricsTimer?.Restart();
        }
        
        public void Stop()
        {
            _metricsTimer?.Stop();
        }

        public void Dispose()
        {
            _metricsTimer?.Dispose();
            _tracingTimer?.Dispose();
        }
    }
}
