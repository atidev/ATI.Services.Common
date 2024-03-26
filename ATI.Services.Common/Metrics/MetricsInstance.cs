using System;
using System.Runtime.CompilerServices;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Variables;
using Microsoft.AspNetCore.Http;
using Prometheus;

namespace ATI.Services.Common.Metrics;

public class MetricsInstance
{
    private readonly string _className;
    private readonly string _externalHttpServiceName;
    private readonly LogSource _logSource;
    private readonly TimeSpan _longRequestTime;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    private static readonly string MachineName = Environment.MachineName;
    
    private Summary Summary { get; }
    
    private static readonly QuantileEpsilonPair[] SummaryQuantileEpsilonPairs = {
        new(0.5, 0.05),
        new(0.9, 0.05),
        new(0.95, 0.01),
        new(0.99, 0.005),
    };

    public MetricsInstance(
        IHttpContextAccessor httpContextAccessor,
        string className,
        LogSource logSource,
        string summaryServiceName,
        string externalHttpServiceName,
        TimeSpan longRequestTime,
        params string[] summaryLabelNames)
    {
        _httpContextAccessor = httpContextAccessor;
        _className = className;
        _externalHttpServiceName = externalHttpServiceName;
        _longRequestTime = longRequestTime;
        _logSource = logSource;

        if (summaryServiceName == null) return;
        
        var options = new SummaryConfiguration
        {
            MaxAge = TimeSpan.FromMinutes(1),
            Objectives = SummaryQuantileEpsilonPairs
        };

        Summary = Prometheus.Metrics.CreateSummary(
            summaryServiceName,
            string.Empty,
            summaryLabelNames,
            options);
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
            _className,
            actionName,
            entityName,
            _externalHttpServiceName,
            HttpContextHelper.MetricsHeadersValues(_httpContextAccessor),
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
            _className,
            actionName,
            entityName,
            _externalHttpServiceName,
            HttpContextHelper.MetricsHeadersValues(_httpContextAccessor),
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
            _className,
            actionName,
            entityName,
            _externalHttpServiceName,
            HttpContextHelper.MetricsHeadersValues(_httpContextAccessor),
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
            _className,
            actionName,
            entityName,
            _externalHttpServiceName,
            HttpContextHelper.MetricsHeadersValues(_httpContextAccessor),
            additionalLabels);

        return new MetricsTimer(Summary, labels);
    }
    
    /// <summary>
    /// Метод управляющий порядком значений лэйблов 
    /// </summary>
    private static string[] ConcatLabelValues(
        string className,
        string actionName,
        string entityName = null,
        string externHttpService = null,
        string[] userLabels = null,
        params string[] additionalLabels)
    {
        return MetricsHelper.ConcatLabels(
            className,
            MachineName,
            actionName,
            entityName,
            externHttpService,
            userLabels,
            additionalLabels);
    }
}