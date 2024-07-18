using System;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Metrics.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ATI.Services.Common.Metrics;

public class MetricsFactory : IMetricsFactory
{
    public const string Prefix = "common_metric";
    private static TimeSpan _defaultLongRequestTime = TimeSpan.FromSeconds(1);
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public MetricsFactory(IOptions<MetricsOptions> options, IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
        if (options.Value.DefaultLongRequestTime != null)
        {
            _defaultLongRequestTime = options.Value.DefaultLongRequestTime.Value;
        }
    }

    [PublicAPI]
    public MetricsInstance CreateHttpClientMetricsFactory(
        [NotNull] string className,
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

        return new MetricsInstance(
            _httpContextAccessor,
            className,
            LogSource.HttpClient,
            $"{Prefix}_http_client",
            externalHttpServiceName,
            longRequestTime ?? _defaultLongRequestTime,
            labels);
    }

    public MetricsInstance CreateRedisMetricsFactory(
        [NotNull] string className,
        TimeSpan? longRequestTime = null,
        params string[] additionalSummaryLabels)
    {
        var labels = ConcatLabelNames(
            "method_name",
            "entity_name",
            null,
            MetricsLabelsAndHeaders.UserLabels,
            additionalSummaryLabels);

        return new MetricsInstance(
            _httpContextAccessor,
            className,
            LogSource.Redis,
            $"{Prefix}_redis",
            null,
            longRequestTime ?? _defaultLongRequestTime,
            labels);
    }

    [PublicAPI]
    public MetricsInstance CreateMongoMetricsFactory(
        [NotNull] string className,
        params string[] additionalSummaryLabels)
    {
        var labels = ConcatLabelNames(
            "method_name",
            "entity_name",
            null,
            MetricsLabelsAndHeaders.UserLabels,
            additionalSummaryLabels);

        return new MetricsInstance(
            _httpContextAccessor,
            className,
            LogSource.Mongo,
            $"{Prefix}_mongo",
            null,
            _defaultLongRequestTime,
            labels);
    }

    public MetricsInstance CreateSqlMetricsFactory(
        [NotNull] string className,
        TimeSpan? longTimeRequest = null,
        params string[] additionalSummaryLabels)
    {
        var labels = ConcatLabelNames(
            "procedure_name",
            "entity_name",
            null,
            MetricsLabelsAndHeaders.UserLabels,
            additionalSummaryLabels);

        return new MetricsInstance(
            _httpContextAccessor,
            className,
            LogSource.Sql,
            $"{Prefix}_sql",
            null,
            longTimeRequest ?? _defaultLongRequestTime,
            labels);
    }

    public MetricsInstance CreateControllerMetricsFactory(
        [NotNull] string className,
        params string[] additionalSummaryLabels)
    {
        var labels = ConcatLabelNames(
            "route_template",
            "entity_name",
            null,
            MetricsLabelsAndHeaders.UserLabels,
            additionalSummaryLabels);

        return new MetricsInstance(
            _httpContextAccessor,
            className,
            LogSource.Controller,
            $"{Prefix}_controller",
            null,
            _defaultLongRequestTime,
            labels);
    }

    [PublicAPI]
    public MetricsInstance CreateRepositoryMetricsFactory(
        [NotNull] string className,
        TimeSpan? requestLongTime = null,
        params string[] additionalSummaryLabels)
    {
        var labels = ConcatLabelNames(
            "method_name",
            "entity_name",
            null,
            MetricsLabelsAndHeaders.UserLabels,
            additionalSummaryLabels);

        return new MetricsInstance(
            _httpContextAccessor,
            className,
            LogSource.Repository,
            $"{Prefix}_repository",
            null,
            requestLongTime ?? _defaultLongRequestTime,
            labels);
    }
        
    [PublicAPI]
    public MetricsInstance CreateRabbitMqMetricsFactory(
        RabbitMetricsType type,
        [NotNull] string className,
        TimeSpan? requestLongTime = null,
        params string[] additionalSummaryLabels)
    {
        var labels = ConcatLabelNames(
            "exchange_routing_key_name",
            "entity_name",
            null,
            MetricsLabelsAndHeaders.UserLabels,
            additionalSummaryLabels);

        return new MetricsInstance(
            _httpContextAccessor,
            className,
            LogSource.RabbitMq,
            $"{Prefix}_rabbitmq_{type.ToString().ToLower()}",
            null,
            requestLongTime ?? _defaultLongRequestTime,
            labels);
    }
     
    [PublicAPI]
    public MetricsInstance CreateCustomMetricsFactory(
        [NotNull] string className,
        string customMetricName,
        TimeSpan? requestLongTime = null,
        params string[] additionalSummaryLabels)
    {
        if(customMetricName is null) throw new ArgumentNullException(nameof(customMetricName));
            
        var labels = ConcatLabelNames(
            "method_name",
            "entity_name",
            null,
            MetricsLabelsAndHeaders.UserLabels,
            additionalSummaryLabels);

        return new MetricsInstance(
            _httpContextAccessor,
            className,
            LogSource.Custom,
            $"{Prefix}_{customMetricName}",
            null,
            requestLongTime ?? _defaultLongRequestTime,
            labels);
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
        return MetricsHelper.ConcatLabels(
            "class_name",
            "machine_name",
            actionName,
            entityName,
            externHttpService,
            userLabels,
            additionalLabels);
    }
}