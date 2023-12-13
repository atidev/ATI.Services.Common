using System;
using System.Collections.Generic;
using System.Diagnostics;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Serializers;
using Newtonsoft.Json;
using NLog;
using Prometheus;

namespace ATI.Services.Common.Metrics
{
    public class MetricsTimer : IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly Summary _summary;
        private readonly Stopwatch _stopwatch;
        private readonly string[] _summaryLabels;
        private readonly TimeSpan? _longRequestTime;
        private readonly object _context;
        private readonly LogSource? _logSource;
        private readonly ISerializer _serializer;

        /// <summary>
        /// Конструктор таймера метрик, который считает только метрику (время выполнения + счётчик) для прометеуса
        /// </summary>
        public MetricsTimer(
            Summary summary,
            string[] additionSummaryLabels,
            TimeSpan? longRequestTime = null,
            object context = null,
            LogSource? logSource = null,
            bool startTimerImmediately = true)
        {
            _summary = summary;
            _summaryLabels = additionSummaryLabels;

            _stopwatch = new Stopwatch();
            if (startTimerImmediately)
            {
                _stopwatch.Start();
            }
            _longRequestTime = longRequestTime;
            _context = context;
            _logSource = logSource;
            _serializer = SerializerFactory.GetSerializerByType(SerializerType.SystemTextJson);
        }

        public void Restart()
        {
            _stopwatch.Restart();
        }

        public void Stop()
        {
            _stopwatch.Stop();
        }

        public void Dispose()
        {
            if (_summary == null)
            {
                return;
            }

            if (_summaryLabels == null)
            {
                _summary.Observe(_stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _summary.Labels(_summaryLabels).Observe(_stopwatch.ElapsedMilliseconds);
            }

            _stopwatch.Stop();

            if (_longRequestTime == null
                || !(_stopwatch.Elapsed > _longRequestTime)
                || _context == null
                || _logSource == null)
            {
                return;
            }
            
            Logger.LogWithObject(LogLevel.Warn, null, "Long request WARN.", GetContext());
        }

        private Dictionary<object, object> GetContext()
        {
            var settings = new JsonSerializerSettings()
            {
                ContractResolver = SensitiveDataContractResolver.Instance
            };
            _serializer.SetSerializeSettings(settings);
            
            var metricString = _serializer.Serialize(
                new
                {
                    LogSource = _logSource,
                    RequestTime = _stopwatch.Elapsed,
                    Labels = _summaryLabels,
                    Context = _context
                });

            return new Dictionary<object, object>
            {
                { "metricSource", _logSource.Value.ToString() },
                { "metricString", metricString }
            };
        }
    }
}