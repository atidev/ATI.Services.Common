using System;
using System.Runtime.CompilerServices;

namespace ATI.Services.Common.Metrics.Interfaces;

public interface IMetricsInstance
{
    public IDisposable CreateMetricsTimerWithLogging(
        string entityName,
        [CallerMemberName] string actionName = null,
        object requestParams = null,
        TimeSpan? longRequestTime = null,
        params string[] additionalLabels);

    public MetricsTimer CreateMetricsTimerWithDelayedLogging(
        string entityName,
        [CallerMemberName] string actionName = null,
        object requestParams = null,
        TimeSpan? longRequestTime = null,
        params string[] additionalLabels);

    public IDisposable CreateLoggingMetricsTimer(
        string entityName,
        [CallerMemberName] string actionName = null,
        object requestParams = null,
        TimeSpan? longRequestTime = null,
        params string[] additionalLabels);

    public IDisposable CreateMetricsTimer(
        string entityName,
        [CallerMemberName] string actionName = null,
        params string[] additionalLabels);
    
    

}