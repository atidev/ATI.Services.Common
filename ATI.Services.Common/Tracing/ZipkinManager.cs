using System;
using ATI.Services.Common.Metrics;
using NLog;
using zipkin4net;
using zipkin4net.Tracers.Zipkin;
using zipkin4net.Transport.Http;

namespace ATI.Services.Common.Tracing
{
    public class ZipkinManager : IDisposable
    {
        public TracingOptions Options { get; private set; }

        public void Init(TracingOptions options)
        {
            Options = options;
            if (Options.Enabled)
            {
                var logger = new MetricsLogger();
                TraceManager.SamplingRate = Options.Rate;
                TraceManager.RegisterTracer(new ZipkinTracer(new HttpZipkinSender(Options.TraceEndpoint, "application/json"), new JSONSpanSerializer(), new Statistics()));
                TraceManager.Start(logger);
            }
            LogManager.Configuration.AddTarget(new ZipkinLogTarget());
            LogManager.Configuration.AddRule(LogLevel.Warn, LogLevel.Error, nameof(ZipkinLogTarget));
            LogManager.ReconfigExistingLoggers();
        }

        public void Dispose()
        {
            TraceManager.Stop();
        }
    }
}