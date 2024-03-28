using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Common.Metrics;

[PublicAPI]
public class MeasureAttribute : ActionFilterAttribute
{
    private string _metricEntity;
    private readonly TimeSpan? _longRequestTime;
    private readonly bool _longRequestLoggingEnabled = true;
        
    private static readonly ConcurrentDictionary<string, MetricsInstance> ControllerNameMetricsInstances
        = new();

    public MeasureAttribute() { }

    public MeasureAttribute(string metricEntity) => _metricEntity = metricEntity;

    /// <param name="metricEntity">Имя метрики</param>
    /// <param name="longRequestTimeSeconds">Время ответа после которого запрос считается достаточно долгим для логирования. В секундах</param>
    public MeasureAttribute(string metricEntity, double longRequestTimeSeconds) : this(metricEntity) =>
        _longRequestTime = TimeSpan.FromSeconds(longRequestTimeSeconds);

    public MeasureAttribute(string metricEntity, bool longRequestLoggingEnabled) : this(metricEntity) =>
        _longRequestLoggingEnabled = longRequestLoggingEnabled;

    public MeasureAttribute(double longRequestTimeSeconds) =>
        _longRequestTime = TimeSpan.FromSeconds(longRequestTimeSeconds);

    public MeasureAttribute(bool longRequestLoggingEnabled)
    {
        _longRequestLoggingEnabled = longRequestLoggingEnabled;
    }

    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var metricsFactory = context.HttpContext.RequestServices.GetService<MetricsFactory>();
        
        var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
        var actionName =
            $"{context.HttpContext.Request.Method}:{controllerActionDescriptor.AttributeRouteInfo.Template}";
        var controllerName = controllerActionDescriptor.ControllerName + "Controller";

        if (string.IsNullOrEmpty(_metricEntity))
        {
            _metricEntity = controllerActionDescriptor.ControllerName;
        }

        var metricsInstance =
            ControllerNameMetricsInstances.GetOrAdd(
                controllerName,
                metricsFactory.CreateControllerMetricsFactory(controllerName));

        using (CreateTimer(context, metricsInstance, actionName))
        {
            await next.Invoke();
        }
    }

    private IDisposable CreateTimer(ActionExecutingContext context, MetricsInstance metricsInstance, string actionName)
    {
        return _longRequestLoggingEnabled
                   ? metricsInstance.CreateLoggingMetricsTimer(_metricEntity, actionName, context.ActionArguments, _longRequestTime)
                   : metricsInstance.CreateMetricsTimer(_metricEntity, actionName);
    }
}