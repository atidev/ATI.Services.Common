using System.Threading.Tasks;
using ATI.Services.Common.Initializers;
using ATI.Services.Common.Initializers.Interfaces;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using zipkin4net;
using zipkin4net.Tracers.Zipkin;
using zipkin4net.Transport.Http;

namespace ATI.Services.Common.Tracing
{
    [PublicAPI]
    [InitializeOrder(Order = InitializeOrder.First)]
    public sealed class TracingInitializer : IInitializer
    {
        private readonly TracingOptions _options;
        private readonly ILoggerFactory _loggerFactory;

        public TracingInitializer(IOptions<TracingOptions> options, ILoggerFactory loggerFactory)
        {
            _options = options.Value;
            _loggerFactory = loggerFactory;
        }

        public Task InitializeAsync()
        {
            TraceManager.SamplingRate = _options.Rate;

            var httpSender = new HttpZipkinSender(_options.TraceEndpoint, "application/json");
            var tracer = new ZipkinTracer(httpSender, new JSONSpanSerializer(), new Statistics());

            TraceManager.RegisterTracer(tracer);

            return Task.CompletedTask;
        }

        public void Start()
        {
            TraceManager.Start(new TracingLogger(_loggerFactory, "zipkin_tracing"));
        }
        public void Stop()
        {
            TraceManager.Stop();
        }
    }
}
