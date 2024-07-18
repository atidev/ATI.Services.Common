using System;
using JetBrains.Annotations;

namespace ATI.Services.Common.Metrics.Interfaces;

public interface IMetricsFactory
{
    public MetricsInstance CreateHttpClientMetricsFactory([NotNull] string className,
        string externalHttpServiceName,
        TimeSpan? longRequestTime = null,
        params string[] additionalSummaryLabels);

    public MetricsInstance CreateRedisMetricsFactory(
        [NotNull] string className,
        TimeSpan? longRequestTime = null,
        params string[] additionalSummaryLabels);

    public MetricsInstance CreateMongoMetricsFactory(
        [NotNull] string className,
        params string[] additionalSummaryLabels);

    public MetricsInstance CreateSqlMetricsFactory(
        [NotNull] string className,
        TimeSpan? longTimeRequest = null,
        params string[] additionalSummaryLabels);

    public MetricsInstance CreateControllerMetricsFactory(
        [NotNull] string className,
        params string[] additionalSummaryLabels);

    public MetricsInstance CreateRepositoryMetricsFactory(
        [NotNull] string className,
        TimeSpan? requestLongTime = null,
        params string[] additionalSummaryLabels);

    public MetricsInstance CreateRabbitMqMetricsFactory(
        RabbitMetricsType type,
        [NotNull] string className,
        TimeSpan? requestLongTime = null,
        params string[] additionalSummaryLabels);

    public MetricsInstance CreateCustomMetricsFactory(
        [NotNull] string className,
        string customMetricName,
        TimeSpan? requestLongTime = null,
        params string[] additionalSummaryLabels);
    
    
}