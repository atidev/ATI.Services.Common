using NLog;
using Prometheus;

namespace ATI.Services.Common.Metrics
{
    public class MetricsLogger : zipkin4net.ILogger //TraceLogger
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private static readonly Counter ErrorCounter = Prometheus.Metrics.CreateCounter("ErrorCounter", "");

        public void LogInformation(string message)
        {
            Logger.Info(message);
        }

        public void LogWarning(string message)
        {
            Logger.Warn(message);
        }

        public void LogError(string message)
        {
            ErrorCounter.Inc();
            Logger.Error(message);
        }
    }
}
