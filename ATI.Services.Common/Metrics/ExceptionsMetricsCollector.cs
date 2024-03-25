using System;
using System.Collections.Concurrent;
using System.Diagnostics.Tracing;
using Prometheus;

namespace ATI.Services.Common.Metrics;

public class ExceptionsMetricsCollector : EventListener
{
    // Да, даже под linux - Microsoft-Windows
    private const string ExceptionSourceName = "Microsoft-Windows-DotNETRuntime";
    private const int ExceptionBit = 0x8000;
    private const int ExceptionNameIndex = 0;
    private readonly ConcurrentDictionary<string, long> _exceptionCounters = new();
    private MetricFactory _metricFactory;
    private Gauge _gauge;

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
        _gauge = _metricFactory.CreateGauge($"{MetricsFactory.Prefix}_Exceptions", "", "exception_type");
    }

    public void UpdateMetrics()
    {
        foreach (var exceptionType in _exceptionCounters.Keys)
        {
            _exceptionCounters.TryRemove(exceptionType, out var count);
            _gauge.WithLabels(exceptionType).Set(count);
        }
    }
}