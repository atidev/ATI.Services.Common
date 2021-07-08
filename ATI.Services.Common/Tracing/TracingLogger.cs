using Microsoft.Extensions.Logging;

namespace ATI.Services.Common.Tracing
{
    internal class TracingLogger : zipkin4net.ILogger
    {
        private readonly ILogger _logger;

        public TracingLogger(ILoggerFactory loggerFactory, string loggerName)
        {
            _logger = loggerFactory.CreateLogger(loggerName);
        }
        public void LogError(string message)
        {
            _logger.LogError(message);
        }
        public void LogInformation(string message)
        {
            _logger.LogInformation(message);
        }
        public void LogWarning(string message)
        {
            _logger.LogWarning(message);
        }
    }
}
