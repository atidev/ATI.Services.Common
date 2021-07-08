using NLog;
using NLog.Targets;
using zipkin4net;

namespace ATI.Services.Common.Tracing
{
    public sealed class ZipkinLogTarget : TargetWithLayout
    {
        public ZipkinLogTarget()
        {
            Name = nameof(ZipkinLogTarget);
        }

        protected override void Write(LogEventInfo logEvent)
        {
            var trace = Trace.Current;
            trace?.Record(Annotations.Tag("error", Layout.Render(logEvent)));
        }
    }
}
