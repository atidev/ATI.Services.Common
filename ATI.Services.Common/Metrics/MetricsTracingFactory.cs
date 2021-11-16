using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ATI.Services.Common.Logging;
using JetBrains.Annotations;
using Prometheus;
using zipkin4net;

namespace ATI.Services.Common.Metrics
{
    [PublicAPI]
    public class MetricsTracingFactory
    {
        private Summary Summary { get; }
        private static string _serviceName = "default_name";
        private static readonly string MachineName = Environment.MachineName;
        private readonly string _tracingServiceName;
        private readonly string _externalHttpServiceName;
        private readonly LogSource _logSource;
        private static readonly string ClientLabelName = "client_name";
        private static TimeSpan _defaultLongRequestTime = TimeSpan.FromSeconds(1);

        //Время запроса считающегося достаточно долгим, что бы об этом доложить в кибану
        private readonly TimeSpan _longRequestTime;

        public static void Init(string serviceName, TimeSpan? defaultLongRequestTime = null)
        {
            _serviceName = serviceName;
            if (defaultLongRequestTime != null)
                _defaultLongRequestTime = defaultLongRequestTime.Value;
        }

        public static MetricsTracingFactory CreateHttpClientMetricsFactory([NotNull]string summaryName, string externalHttpServiceName, TimeSpan? longRequestTime = null, params string[] additionalSummaryLabels)
        {
            var labels = ConcatLabelNames("route_template", "entity_name", "external_http_service_name", ClientLabelName, additionalSummaryLabels);
            
            return new MetricsTracingFactory(
                LogSource.HttpClient,
                _serviceName + summaryName,
                externalHttpServiceName,
                externalHttpServiceName,
                longRequestTime,
                labels);
        }

        public static MetricsTracingFactory CreateRedisMetricsFactory([NotNull]string summaryName, TimeSpan? longRequestTime = null, params string[] additionalSummaryLabels)
        {
            var labels = ConcatLabelNames("method_name", "entity_name", null, ClientLabelName, additionalSummaryLabels);

            return new MetricsTracingFactory(
                LogSource.Redis,
                _serviceName + summaryName,
                longRequestTime ?? _defaultLongRequestTime,
                $"{_serviceName}_{summaryName}",
                labels);
        }

        public static MetricsTracingFactory CreateMongoMetricsFactory([NotNull]string summaryName, params string[] additionalSummaryLabels)
        {
            var labels = ConcatLabelNames("method_name", "entity_name", null, ClientLabelName, additionalSummaryLabels);

            return new MetricsTracingFactory(
                LogSource.Mongo,
                _serviceName + summaryName,
                _defaultLongRequestTime,
                $"{_serviceName}_{summaryName}",
                labels);
        }

        public static MetricsTracingFactory CreateSqlMetricsFactory([NotNull]string summaryName, TimeSpan? longTimeRequest = null, params string[] additionalSummaryLabels)
        {
            var labels = ConcatLabelNames("procedure_name", "entity_name", null, ClientLabelName, additionalSummaryLabels);

            return new MetricsTracingFactory(
                LogSource.Sql,
                _serviceName + summaryName,
                _defaultLongRequestTime,
                $"{_serviceName}_{summaryName}",
                labels);
        }

        public static MetricsTracingFactory CreateControllerMetricsFactory(
            [NotNull]string summaryName,
            double? longRequestTime = null,
            params string[] additionalSummaryLabels)
        {
            var labels = ConcatLabelNames("route_template", "entity_name", null, ClientLabelName, additionalSummaryLabels);

            return new MetricsTracingFactory(
                LogSource.Controller,
                _serviceName + summaryName,
                longRequestTime == null ? _defaultLongRequestTime : TimeSpan.FromSeconds(longRequestTime.Value),
                $"{_serviceName}_{summaryName}",
                labels);
        }

        public static MetricsTracingFactory CreateRepositoryMetricsFactory([NotNull]string summaryName, TimeSpan? requestLongTime = null, params string[] additionalSummaryLabels)
        {
            var labels = ConcatLabelNames("method_name", "entity_name", null, ClientLabelName, additionalSummaryLabels);

            return new MetricsTracingFactory(
                LogSource.Repository,
                _serviceName + summaryName,
                requestLongTime ?? _defaultLongRequestTime,
                $"{_serviceName}_{summaryName}",
                labels);
        }

        public static MetricsTracingFactory CreateTracingFactory([NotNull] string tracingService, TimeSpan? longRequestTime = null)
        {
            return new(LogSource.Tracing, null, longRequestTime ?? _defaultLongRequestTime, tracingService);
        }

        private MetricsTracingFactory(
            LogSource logSource,
            string summaryServiceName,
            string externalHttpServiceName,
            string tracingServiceName = null,
            TimeSpan? longRequestTime = null,
            params string[] summaryLabelNames)
        {
            _tracingServiceName = tracingServiceName;
            _externalHttpServiceName = externalHttpServiceName;
            _longRequestTime = longRequestTime ?? _defaultLongRequestTime;
            _logSource = logSource;

            if (summaryServiceName != null)
                Summary = Prometheus.Metrics.CreateSummary(summaryServiceName, string.Empty, summaryLabelNames, null,
                    TimeSpan.FromMinutes(1), null, null);
        }

        private MetricsTracingFactory(
            LogSource logSource,
            [CanBeNull]string summaryServiceName,
            TimeSpan longRequestTime,
            string tracingServiceName = null,
            params string[] summaryLabelNames)
        {
            _tracingServiceName = tracingServiceName;
            _longRequestTime = longRequestTime;
            _logSource = logSource;

            if (summaryServiceName != null)
                Summary = Prometheus.Metrics.CreateSummary(summaryServiceName, string.Empty, summaryLabelNames, null,
                    TimeSpan.FromMinutes(1), null, null);
        }

        public IDisposable CreateTracingWithLoggingMetricsTimer(
            [NotNull]Dictionary<string, string> getTracingCallback,
            string entityName,
            [CallerMemberName] string actionName = null,
            object requestParams = null,
            TimeSpan? longRequestTime = null,
            params string[] additionalLabels)
        {
            if (string.IsNullOrEmpty(_tracingServiceName))
            {
                throw new NullReferenceException($"{nameof(_tracingServiceName)} is not initialized");
            }

            
            if (Summary == null)
            {
                throw new NullReferenceException($"{nameof(Summary)} is not initialized");
            }

            var tracingTimer = new TracingTimer(Trace.Current, _tracingServiceName, actionName, getTracingCallback);
            var metricTimer = new MetricsTimer(
                Summary,
                ConcatLabelValues(
                    actionName,
                    entityName,
                    _externalHttpServiceName,
                    AppHttpContext.ClientName,
                    additionalLabels),
                longRequestTime ?? _longRequestTime,
                requestParams,
                _logSource);

            return new TimersWrapper(metricTimer, tracingTimer);
        }
        
        /// <summary>
        /// В случае создания данного экземпляра таймер для метрик стартует не сразу, а только после вызова метода Restart()
        /// </summary>
        /// <param name="getTracingCallback"></param>
        /// <param name="entityName"></param>
        /// <param name="actionName"></param>
        /// <param name="requestParams"></param>
        /// <param name="longRequestTime"></param>
        /// <param name="additionalLabels"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public TimersWrapper CreateTracingWithDelayedLoggingMetricsTimer(
            [NotNull]Dictionary<string, string> getTracingCallback,
            string entityName,
            [CallerMemberName] string actionName = null,
            object requestParams = null,
            TimeSpan? longRequestTime = null,
            params string[] additionalLabels)
        {
            if (string.IsNullOrEmpty(_tracingServiceName))
            {
                throw new NullReferenceException($"{nameof(_tracingServiceName)} is not initialized");
            }

            if (Summary == null)
            {
                throw new NullReferenceException($"{nameof(Summary)} is not initialized");
            }

            var tracingTimer = new TracingTimer(Trace.Current, _tracingServiceName, actionName, getTracingCallback);
            var metricTimer = new MetricsTimer(
                Summary,
                ConcatLabelValues(
                    actionName,
                    entityName,
                    _externalHttpServiceName,
                    AppHttpContext.ClientName,
                    additionalLabels),
                longRequestTime ?? _longRequestTime,
                requestParams,
                _logSource,
                false);

            return new TimersWrapper(metricTimer, tracingTimer);
        }

        public IDisposable CreateTracingWithLoggingMetricsTimerOnExistingTrace(
            [NotNull]Trace trace,
            string entityName,
            [CallerMemberName]string actionName = null,
            IDictionary<string, object> requestParams = null,
            params string[] additionalLabels)
        {
            if (string.IsNullOrEmpty(_tracingServiceName))
            {
                throw new NullReferenceException($"{nameof(_tracingServiceName)} is not initialized");
            }

            if (Summary == null)
            {
                throw new NullReferenceException($"{nameof(Summary)} is not initialized");
            }

            var tracingTimer = new TracingTimer(trace, _tracingServiceName, actionName);
            var metricTimer = new MetricsTimer(
                Summary,
                ConcatLabelValues(
                    actionName,
                    entityName,
                    _externalHttpServiceName,
                    AppHttpContext.ClientName,
                    additionalLabels),
                _longRequestTime,
                requestParams,
                _logSource);


            return new TimersWrapper(metricTimer, tracingTimer);
        }

        public IDisposable CreateTracingMetricsTimerOnExistingTrace(
            [NotNull]Trace trace,
            string entityName,
            [CallerMemberName]string actionName = null,
            params string[] additionalLabels)
        {
            if (string.IsNullOrEmpty(_tracingServiceName))
            {
                throw new NullReferenceException($"{nameof(_tracingServiceName)} is not initialized");
            }

            if (Summary == null)
            {
                throw new NullReferenceException($"{nameof(Summary)} is not initialized");
            }

            var tracingTimer = new TracingTimer(trace, _tracingServiceName, actionName);
            var metricTimer = new MetricsTimer(
                Summary,
                ConcatLabelValues(
                    actionName,
                    entityName,
                    _externalHttpServiceName,
                    AppHttpContext.ClientName,
                    additionalLabels));


            return new TimersWrapper(metricTimer, tracingTimer);
        }

        public IDisposable CreateTracingTimer([NotNull]Dictionary<string, string> getTracingTagsCallback, [CallerMemberName] string actionName = null)
        {
            if (string.IsNullOrEmpty(_tracingServiceName))
            {
                throw new NullReferenceException($"{nameof(_tracingServiceName)} is not initialized");
            }

            var tracingTimer = new TracingTimer(Trace.Current, _tracingServiceName, actionName, getTracingTagsCallback);

            return new TimersWrapper(tracingTimer);
        }


        public IDisposable CreateLoggingMetricsTimer(
            string entityName,
            [CallerMemberName]string actionName = null,
            object requestParams = null,
            params string[] additionalLabels)
        {
            
            var metricsTimer =
                new MetricsTimer(
                    Summary,
                    ConcatLabelValues(
                        actionName,
                        entityName,
                        _externalHttpServiceName,
                        AppHttpContext.ClientName,
                        additionalLabels),
                    _longRequestTime,
                    requestParams,
                    _logSource);

            return new TimersWrapper(metricsTimer);
        }

        public IDisposable CreateMetricsTimer(
            string entityName,
            [CallerMemberName]string actionName = null,
            params string[] additionalLabels)
        {
            var metricsTimer =
                new MetricsTimer(
                    Summary,
                    ConcatLabelValues(
                        actionName,
                        entityName,
                        _externalHttpServiceName,
                        AppHttpContext.ClientName,
                        additionalLabels));

            return new TimersWrapper(metricsTimer);
        }

        /// <summary>
        /// Метод управляющий порядком значений лэйблов 
        /// </summary>
        private static string[] ConcatLabelValues(
            string actionName,
            string entityName = null,
            string externHttpService = null,
            string clientName = null,
            params string[] additionalLabels)
        {
            return ConcatLabels(MachineName, actionName, entityName, externHttpService, clientName, additionalLabels);
        }

        /// <summary>
        /// Метод управляющий порядком названий лэйблов
        /// </summary>
        private static string[] ConcatLabelNames(
            string actionName,
            string entityName = null,
            string externHttpService = null,
            string clientName = null,
            params string[] additionalLabels)
        {
            return ConcatLabels("machine_name", actionName, entityName, externHttpService, clientName, additionalLabels);
        }

        /// <summary>
        /// Метод управляющий порядком лэйблов и их значений
        /// <param name="actionName"> Указывать только при объявлении лейблов. Записывается он в таймере, так как нужен для трейсинга</param>
        /// </summary>
        private static string[] ConcatLabels(
            string actionName,
            string machineName,
            string entityName,
            string externHttpService,
            string clientName,
            params string[] additionalLabels)
        {
            var labels = new List<string>(4);

            labels.Add(machineName);

            if (actionName != null)
                labels.Add(actionName);

            if (entityName != null)
                labels.Add(entityName);

            if (externHttpService != null)
                labels.Add(externHttpService);

            if (clientName != null)
                labels.Add(clientName);

            if (additionalLabels.Length != 0)
                labels.AddRange(additionalLabels);

            return labels.ToArray();
        }
    }
}