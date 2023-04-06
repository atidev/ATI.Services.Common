using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Variables;
using JetBrains.Annotations;
using Prometheus;

namespace ATI.Services.Common.Metrics
{
    [PublicAPI]
    public class MetricsFactory
    {
        private Summary Summary { get; }
        private static string _serviceName = "default_name";
        private static readonly string MachineName = Environment.MachineName;
        private readonly string _externalHttpServiceName;
        private readonly LogSource _logSource;
        private static TimeSpan _defaultLongRequestTime = TimeSpan.FromSeconds(1);

        //Время запроса считающегося достаточно долгим, что бы об этом доложить в кибану
        private readonly TimeSpan _longRequestTime;

        public static void Init(string serviceName, TimeSpan? defaultLongRequestTime = null)
        {
            _serviceName = serviceName;
            if (defaultLongRequestTime != null)
            {
                _defaultLongRequestTime = defaultLongRequestTime.Value;
            }
        }

        public static MetricsFactory CreateHttpClientMetricsFactory(
            [NotNull] string summaryName,
            string externalHttpServiceName,
            TimeSpan? longRequestTime = null,
            params string[] additionalSummaryLabels)
        {
            var labels = ConcatLabelNames(
                "route_template",
                "entity_name",
                "external_http_service_name",
                MetricsLabelsAndHeaders.UserLabels,
                additionalSummaryLabels);

            return new MetricsFactory(
                LogSource.HttpClient,
                _serviceName + summaryName,
                externalHttpServiceName,
                longRequestTime,
                labels);
        }

        public static MetricsFactory CreateRedisMetricsFactory(
            [NotNull] string summaryName,
            TimeSpan? longRequestTime = null,
            params string[] additionalSummaryLabels)
        {
            var labels = ConcatLabelNames(
                "method_name",
                "entity_name",
                null,
                MetricsLabelsAndHeaders.UserLabels,
                additionalSummaryLabels);

            return new MetricsFactory(
                LogSource.Redis,
                _serviceName + summaryName,
                longRequestTime ?? _defaultLongRequestTime,
                labels);
        }

        public static MetricsFactory CreateMongoMetricsFactory(
            [NotNull] string summaryName,
            params string[] additionalSummaryLabels)
        {
            var labels = ConcatLabelNames("method_name", "entity_name", null, MetricsLabelsAndHeaders.UserLabels,
                additionalSummaryLabels);

            return new MetricsFactory(
                LogSource.Mongo,
                _serviceName + summaryName,
                _defaultLongRequestTime,
                labels);
        }

        public static MetricsFactory CreateSqlMetricsFactory(
            [NotNull] string summaryName,
            TimeSpan? longTimeRequest = null,
            params string[] additionalSummaryLabels)
        {
            var labels = ConcatLabelNames(
                "procedure_name",
                "entity_name",
                null,
                MetricsLabelsAndHeaders.UserLabels,
                additionalSummaryLabels);

            return new MetricsFactory(
                LogSource.Sql,
                _serviceName + summaryName,
                _defaultLongRequestTime,
                labels);
        }

        public static MetricsFactory CreateControllerMetricsFactory(
            [NotNull] string summaryName,
            params string[] additionalSummaryLabels)
        {
            var labels = ConcatLabelNames(
                "route_template",
                "entity_name",
                null,
                MetricsLabelsAndHeaders.UserLabels,
                additionalSummaryLabels);

            return new MetricsFactory(
                LogSource.Controller,
                _serviceName + summaryName,
                _defaultLongRequestTime,
                labels);
        }

        public static MetricsFactory CreateRepositoryMetricsFactory(
            [NotNull] string summaryName,
            TimeSpan? requestLongTime = null,
            params string[] additionalSummaryLabels)
        {
            var labels = ConcatLabelNames(
                "method_name",
                "entity_name",
                null,
                MetricsLabelsAndHeaders.UserLabels,
                additionalSummaryLabels);

            return new MetricsFactory(
                LogSource.Repository,
                _serviceName + summaryName,
                requestLongTime ?? _defaultLongRequestTime,
                labels);
        }

        public static MetricsFactory CreateHttpMetricsFactory(TimeSpan? longRequestTime = null)
        {
            return new MetricsFactory(
                LogSource.Metrics, 
                null, 
                longRequestTime ?? _defaultLongRequestTime);
        }

        private MetricsFactory(
            LogSource logSource,
            string summaryServiceName,
            string externalHttpServiceName,
            TimeSpan? longRequestTime = null,
            params string[] summaryLabelNames)
        {
            _externalHttpServiceName = externalHttpServiceName;
            _longRequestTime = longRequestTime ?? _defaultLongRequestTime;
            _logSource = logSource;

            if (summaryServiceName != null)
            {
                var options = new SummaryConfiguration
                {
                    MaxAge = TimeSpan.FromMinutes(1)
                };

                Summary = Prometheus.Metrics.CreateSummary(
                    summaryServiceName,
                    string.Empty,
                    summaryLabelNames,
                    options);
            }
        }

        private MetricsFactory(
            LogSource logSource,
            [CanBeNull] string summaryServiceName,
            TimeSpan longRequestTime,
            params string[] summaryLabelNames)
        {
            _longRequestTime = longRequestTime;
            _logSource = logSource;

            if (summaryServiceName != null)
            {
                var options = new SummaryConfiguration
                {
                    MaxAge = TimeSpan.FromMinutes(1)
                };

                Summary = Prometheus.Metrics.CreateSummary(
                    summaryServiceName,
                    string.Empty,
                    summaryLabelNames,
                    options);
            }
        }

        public IDisposable CreateMetricsTimerWithLogging(
            string entityName,
            [CallerMemberName] string actionName = null,
            object requestParams = null,
            TimeSpan? longRequestTime = null,
            params string[] additionalLabels)
        {
            if (Summary == null)
            {
                throw new NullReferenceException($"{nameof(Summary)} is not initialized");
            }

            var labels = ConcatLabelValues(
                actionName,
                entityName,
                _externalHttpServiceName,
                AppHttpContext.MetricsHeadersValues,
                additionalLabels);

            return new MetricsTimer(
                Summary,
                labels,
                longRequestTime ?? _longRequestTime,
                requestParams,
                _logSource);
        }

        /// <summary>
        /// В случае создания данного экземпляра таймер для метрик стартует не сразу, а только после вызова метода Restart()
        /// </summary>
        /// <param name="entityName"></param>
        /// <param name="actionName"></param>
        /// <param name="requestParams"></param>
        /// <param name="longRequestTime"></param>
        /// <param name="additionalLabels"></param>
        /// <returns></returns>
        /// <exception cref="NullReferenceException"></exception>
        public MetricsTimer CreateMetricsTimerWithDelayedLogging(
            string entityName,
            [CallerMemberName] string actionName = null,
            object requestParams = null,
            TimeSpan? longRequestTime = null,
            params string[] additionalLabels)
        {
            if (Summary == null)
            {
                throw new NullReferenceException($"{nameof(Summary)} is not initialized");
            }

            var labels = ConcatLabelValues(
                actionName,
                entityName,
                _externalHttpServiceName,
                AppHttpContext.MetricsHeadersValues,
                additionalLabels);

            return new MetricsTimer(
                Summary,
                labels,
                longRequestTime ?? _longRequestTime,
                requestParams,
                _logSource,
                false);
        }

        public IDisposable CreateLoggingMetricsTimer(
            string entityName,
            [CallerMemberName] string actionName = null,
            object requestParams = null,
            TimeSpan? longRequestTime = null,
            params string[] additionalLabels)
        {
            var labels = ConcatLabelValues(
                actionName,
                entityName,
                _externalHttpServiceName,
                AppHttpContext.MetricsHeadersValues,
                additionalLabels);

            return new MetricsTimer(
                Summary,
                labels,
                longRequestTime ?? _longRequestTime,
                requestParams,
                _logSource);
        }

        public IDisposable CreateMetricsTimer(
            string entityName,
            [CallerMemberName] string actionName = null,
            params string[] additionalLabels)
        {
            var labels = ConcatLabelValues(
                actionName,
                entityName,
                _externalHttpServiceName,
                AppHttpContext.MetricsHeadersValues,
                additionalLabels);

            return new MetricsTimer(Summary, labels);
        }

        /// <summary>
        /// Метод управляющий порядком значений лэйблов 
        /// </summary>
        private static string[] ConcatLabelValues(
            string actionName,
            string entityName = null,
            string externHttpService = null,
            string[] userLabels = null,
            params string[] additionalLabels)
        {
            return ConcatLabels(
                MachineName,
                actionName,
                entityName,
                externHttpService,
                userLabels,
                additionalLabels);
        }

        /// <summary>
        /// Метод управляющий порядком названий лэйблов
        /// </summary>
        private static string[] ConcatLabelNames(
            string actionName,
            string entityName = null,
            string externHttpService = null,
            string[] userLabels = null,
            params string[] additionalLabels)
        {
            return ConcatLabels(
                "machine_name",
                actionName,
                entityName,
                externHttpService,
                userLabels,
                additionalLabels);
        }

        /// <summary>
        /// Метод управляющий порядком лэйблов и их значений
        /// <param name="actionName"> Указывать только при объявлении лейблов. Записывается он в таймере, так как нужен для трейсинга</param>
        /// </summary>
        private static string[] ConcatLabels(
            string machineName,
            string actionName,
            string entityName,
            string externHttpService,
            string[] userLabels,
            params string[] additionalLabels)
        {
            var labels = new List<string>
            {
                actionName
            };

            if (machineName != null)
                labels.Add(machineName);

            if (entityName != null)
                labels.Add(entityName);

            if (externHttpService != null)
                labels.Add(externHttpService);

            if (userLabels != null && userLabels.Length != 0)
                labels.AddRange(userLabels);

            if (additionalLabels.Length != 0)
                labels.AddRange(additionalLabels);

            return labels.ToArray();
        }
    }
}