using System;
using System.Text;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using zipkin4net;
using zipkin4net.Propagation;

namespace ATI.Services.Common.Tracing
{
    [PublicAPI]
    internal class TracingMiddleware
    {
        private readonly RequestDelegate _next;

        private readonly IExtractor<IHeaderDictionary> _extractor;
        private readonly string _serviceName;

        public TracingMiddleware(RequestDelegate next, IOptions<TracingOptions> options)
        {
            _next = next;

            _extractor = Propagations.B3String.Extractor<IHeaderDictionary>((carrier, key) => carrier[key]);
            _serviceName = options.Value.ServiceName;
        }

        public Task InvokeAsync(HttpContext context)
        {
            var traceContext = _extractor.Extract(context.Request.Headers);
            var trace = CreateFromContext(traceContext);

            Trace.Current = trace;

            return trace.IsSampled
                ? InvokeTraced(context, trace)
                : _next(context);
        }

        private Trace CreateFromContext(ITraceContext traceContext)
        {
            return traceContext == null ? Trace.Create() : Trace.CreateFromId(traceContext);
        }

        private async Task InvokeTraced(HttpContext context, Trace trace)
        {
            var request = context.Request;

            try
            {
                trace.Record(Annotations.ServerRecv());
                trace.Record(Annotations.ServiceName(_serviceName));
                trace.Record(Annotations.Rpc(request.Method));

                trace.Record(Annotations.Tag("http.host", GetHost(request)));
                trace.Record(Annotations.Tag("http.uri", GetDisplayUrl(request)));
                trace.Record(Annotations.Tag("http.path", request.Path));

                await _next(context);
            }
            catch (Exception e)
            {
                trace.Record(Annotations.Tag("error", e.Message));
                throw;
            }
            finally
            {
                trace.Record(Annotations.ServerSend());
            }
        }

        private string GetHost(HttpRequest request)
        {
            var host = request.Host;
            return host.HasValue ? host.ToString() : "unknown";
        }

        private string GetDisplayUrl(HttpRequest request)
        {
            var scheme = request.Scheme;
            var host = GetHost(request);
            var pathBase = request.PathBase.Value;
            var path = request.Path.Value;
            var query = request.QueryString.Value;

            var capacity = scheme.Length + "://".Length + host.Length + pathBase.Length + path.Length + query.Length;

            return new StringBuilder(capacity).Append(request.Scheme).Append("://").Append(host).Append(pathBase).Append(path).Append(query).ToString();
        }
    }
}
