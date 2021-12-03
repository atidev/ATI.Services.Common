using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Tracing;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using zipkin4net;
using zipkin4net.Propagation;

namespace ATI.Services.Common.Metrics
{
    [PublicAPI]
    public class MeasureAttribute : ActionFilterAttribute
    {
        private static ZipkinManager _zipkinManager;
        private string _metricEntity;
        private readonly double? _longRequestTime;
        private readonly bool _longRequestLoggingEnabled = true;


        private static readonly ConcurrentDictionary<string, MetricsTracingFactory> ControllerNameMetricsFactories
            = new();

        public MeasureAttribute()
        {
        }

        public MeasureAttribute(string metricEntity)
        {
            _metricEntity = metricEntity;
        }

        /// <summary>
        /// Метрики метрики, метрики и трейсинг
        /// </summary>
        /// <param name="metricEntity">Имя метрики</param>
        /// <param name="longRequestTimeSeconds">Время ответа после которого запрос считается достаточно долгим для логирования. В секундах</param>
        public MeasureAttribute(string metricEntity, double longRequestTimeSeconds) : this(metricEntity)
        {
            _longRequestTime = longRequestTimeSeconds;
        }

        public MeasureAttribute(string metricEntity, bool longRequestLoggingEnabled) : this(metricEntity)
        {
            _longRequestLoggingEnabled = longRequestLoggingEnabled;
        }
        public MeasureAttribute(double longRequestTimeSeconds)
        {
            _longRequestTime = longRequestTimeSeconds;
        }

        public MeasureAttribute(bool longRequestLoggingEnabled)
        {
            _longRequestLoggingEnabled = longRequestLoggingEnabled;
        }

        public static void Initialize(ZipkinManager zipkinManager)
        {
            _zipkinManager = zipkinManager;
        }

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (_zipkinManager == null)
            {
                throw new ArgumentException("Инициализируй ZIPKIN MANAGER!!!");
            }

            var controllerActionDescriptor = (ControllerActionDescriptor)context.ActionDescriptor;
            var actionName = $"{context.HttpContext.Request.Method}:{controllerActionDescriptor.AttributeRouteInfo.Template}";
            var controllerName = controllerActionDescriptor.ControllerName + "Controller";

            if (string.IsNullOrEmpty(_metricEntity))
            {
                _metricEntity = controllerActionDescriptor.ControllerName;
            }

            var metricsFactory =
                ControllerNameMetricsFactories.GetOrAdd(
                    controllerName,
                    MetricsTracingFactory.CreateControllerMetricsFactory(controllerName, _longRequestTime, "client_header_name"));
            
            var serviceName = "Unknown";
            


            if (context.HttpContext.Items.TryGetValue(CommonBehavior.ServiceNameItemKey, out var serviceNameValue))
            {
                serviceName = serviceNameValue as string;
            }
            

            using (TryGetTrace(context.HttpContext, out var trace)

                ? _longRequestLoggingEnabled
                    ? metricsFactory.CreateTracingWithLoggingMetricsTimerOnExistingTrace(trace, _metricEntity, actionName, context.ActionArguments, serviceName)
                    : metricsFactory.CreateTracingMetricsTimerOnExistingTrace(trace, _metricEntity, actionName, serviceName)

                : _longRequestLoggingEnabled
                    ? metricsFactory.CreateLoggingMetricsTimer(_metricEntity, actionName, context.ActionArguments, serviceName)
                    : metricsFactory.CreateMetricsTimer(_metricEntity, actionName, serviceName))
            {
                await next.Invoke();
            }
        }

        private static bool TryGetTrace(HttpContext httpContext, out Trace trace)
        {
            var traceContext = Extractor.Extract(httpContext.Request.Headers);

            if (!_zipkinManager.Options.Enabled || traceContext == null)
            {
                trace = null;
                return false;
            }

            trace = Trace.Current = Trace.CreateFromId(traceContext);

            trace.Record(Annotations.ServiceName(_zipkinManager.Options.ServiceName));
            trace.Record(Annotations.ServerRecv());
            trace.Record(Annotations.Tag("http.host", httpContext.Request.Host.ToString()));
            trace.Record(Annotations.Tag("http.uri", GetDisplayUrl(httpContext.Request)));
            trace.Record(Annotations.Tag("http.method", httpContext.Request.Method));

            return true;
        }


        private static readonly IExtractor<IHeaderDictionary> Extractor =
            Propagations.B3String.Extractor<IHeaderDictionary>((carrier, key) => carrier?[key]);

        private static string GetDisplayUrl(HttpRequest request)
        {
            var host = request.Host.Value ?? "";
            var pathBase = request.PathBase.Value ?? "";
            var path = request.Path.Value ?? "";
            var query = request.QueryString.Value ?? "";
            return new StringBuilder(request.Scheme.Length + "://".Length + host.Length + pathBase.Length + path.Length + query.Length)
                .Append(request.Scheme)
                .Append("://")
                .Append(host)
                .Append(pathBase)
                .Append(path)
                .Append(query).ToString();
        }
    }
}
