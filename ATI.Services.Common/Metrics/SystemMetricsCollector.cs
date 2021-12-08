using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using Prometheus;
using Prometheus.Advanced;

namespace ATI.Services.Common.Metrics
{
    internal class SystemMetricsCollector : IOnDemandCollector
    {
        private readonly string[] _labelValues = { Environment.MachineName };
        private readonly string[] _labelNames = { "machine_name" };
        private static readonly ILogger Logger = LogManager.GetLogger(nameof(SystemMetricsCollector));
        private readonly List<ICounter> _collectionCounts = new(GC.MaxGeneration + 1);
        private readonly int[] _gcCollectCounts = new int[GC.MaxGeneration + 1];
        private readonly Process _process;
        private IGauge _totalMemory;
        private IGauge _virtualMemorySize;
        private IGauge _workingSet;
        private IGauge _privateMemorySize;
        private ICounter _cpuTotal;
        private IGauge _openHandles;
        private IGauge _startTime;
        private IGauge _numThreads;
        private IGauge _pid;

        public SystemMetricsCollector()
        {
            _process = Process.GetCurrentProcess();
        }

        public void RegisterMetrics(ICollectorRegistry registry)
        {
            MetricFactory metricFactory = Prometheus.Metrics.WithCustomRegistry(registry);
            var counter = metricFactory.CreateCounter("gc_collect_count", "GC collection count",
                _labelNames.Union(new[] { "generation" }).ToArray());
            for (var number = 0; number <= GC.MaxGeneration; number++)
            {
                _collectionCounts.Add(counter.Labels(_labelValues.Union(new[] { number.ToString() }).ToArray()));
            }

            _startTime = metricFactory
                .CreateGauge("start_time", "Start time of the process since unix epoch in seconds", _labelNames)
                .Labels(_labelValues);
            _cpuTotal = metricFactory.CreateCounter("cpu_seconds_total",
                "Total user and system CPU time spent in seconds", _labelNames).Labels(_labelValues);
            _virtualMemorySize = metricFactory.CreateGauge("virtual_bytes", "Process virtual memory size", _labelNames).Labels(_labelValues);
            _workingSet = metricFactory.CreateGauge("working_set", "Process working set", _labelNames).Labels(_labelValues);
            _privateMemorySize = metricFactory.CreateGauge("private_bytes", "Process private memory size", _labelNames).Labels(_labelValues);
            _openHandles = metricFactory.CreateGauge("open_handles", "Number of open handles", _labelNames).Labels(_labelValues);
            _numThreads = metricFactory.CreateGauge("num_threads", "Total number of threads", _labelNames).Labels(_labelValues);
            _pid = metricFactory.CreateGauge("processid", "Process ID", _labelNames).Labels(_labelValues);
            _totalMemory = metricFactory.CreateGauge("totalmemory", "Total known allocated memory", _labelNames).Labels(_labelValues);
            _startTime.Set((_process.StartTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
                .TotalSeconds);
            _pid.Set(_process.Id);
        }

        public void UpdateMetrics()
        {
            try
            {
                _process.Refresh();
                for (int generation = 0; generation < _collectionCounts.Count; generation++)
                {
                    var prevCount = _gcCollectCounts[generation];
                    var currentCount = GC.CollectionCount(generation);
                    _gcCollectCounts[generation] = currentCount;
                    _collectionCounts[generation].Inc(currentCount - prevCount);
                }

                _totalMemory.Set(GC.GetTotalMemory(false));
                _virtualMemorySize.Set(_process.VirtualMemorySize64);
                _workingSet.Set(_process.WorkingSet64);
                _privateMemorySize.Set(_process.PrivateMemorySize64);
                _cpuTotal.Inc(Math.Max(0.0, _process.TotalProcessorTime.TotalSeconds - _cpuTotal.Value));
                _openHandles.Set(_process.HandleCount);
                _numThreads.Set(_process.Threads.Count);
            }
            catch (Exception e)
            {
                Logger.Error(e);
            }
        }
    }
}
