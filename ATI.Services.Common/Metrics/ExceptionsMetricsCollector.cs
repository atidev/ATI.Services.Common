using System;
using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using ATI.Services.Common.Tracing;
using Microsoft.Extensions.Configuration;
using Prometheus;
using ConfigurationManager = ATI.Services.Common.Behaviors.ConfigurationManager;

namespace ATI.Services.Common.Metrics
{
    public class ExceptionsMetricsCollector : EventListener
    {
        // Да, даже под linux - Microsoft-Windows
        private const string ExceptionSourceName = "Microsoft-Windows-DotNETRuntime";
        private const int ExceptionBit = 0x8000;
        private const int ExceptionNameIndex = 0;
        private readonly ConcurrentDictionary<string, long> _exceptionCounters = new();
        private MetricFactory _metricFactory;
        private const string ExceptionsMetricName = "Exceptions";
        private Gauge _gauge;

        private string ServiceName { get; }
        public ExceptionsMetricsCollector()
        {
            ServiceName = ConfigurationManager.GetSection(nameof(TracingOptions)).Get<TracingOptions>().MetricsServiceName;
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name != ExceptionSourceName)
                return;
            
            EnableEvents(
                eventSource,
                EventLevel.Error,
                (EventKeywords) ExceptionBit);
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            var exceptionType = eventData.Payload[ExceptionNameIndex].ToString();
            _exceptionCounters.AddOrUpdate(exceptionType, _ => 1, (_, oldValue) => oldValue + 1);
        }


        public void RegisterMetrics(CollectorRegistry registry)
        {
            _metricFactory = Prometheus.Metrics.WithCustomRegistry(registry);
            _gauge = _metricFactory.CreateGauge(ServiceName + ExceptionsMetricName, "", "machine_name", "exception_type");
        }

        public void UpdateMetrics()
        {
            foreach (var exceptionType in _exceptionCounters.Keys)
            {
                _exceptionCounters.TryRemove(exceptionType, out var count);
                _gauge.WithLabels(Environment.MachineName, exceptionType).Set(count);
            }
        }
    }
}