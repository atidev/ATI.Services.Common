using System;
using System.Collections.Generic;
using zipkin4net;
using Trace = zipkin4net.Trace;

namespace ATI.Services.Common.Metrics
{
    public class TracingTimer : IDisposable
    {
        private readonly Trace _trace;

        public TracingTimer(Trace trace, string tracingServiceName, string methodName)
        {
            _trace = trace;
            _trace.Record(Annotations.ServiceName(tracingServiceName));
            _trace.Record(Annotations.ClientSend());
            _trace.Record(Annotations.Rpc($"{tracingServiceName}_{methodName}"));
        }

        /// <summary>
        /// Конструктор таймера метрик, который трейсит обращения к сервиса к внешним ресурсам
        /// </summary>
        /// <param name="trace"></param>
        /// <param name="tracingServiceName"></param>
        /// <param name="methodName"></param>
        /// <param name="getTracingTagsCallback"></param>
        public TracingTimer(Trace trace, string tracingServiceName, string methodName, Dictionary<string, string> getTracingTagsCallback)
        {
            if ((_trace = trace?.Child()) != null)
            {
                _trace.Record(Annotations.ServiceName(tracingServiceName));
                _trace.Record(Annotations.ClientSend());
                _trace.Record(Annotations.Rpc($"{tracingServiceName}_{methodName}"));

                foreach (var tag in getTracingTagsCallback)
                {
                    if (!string.IsNullOrWhiteSpace(tag.Key) &&
                        !string.IsNullOrWhiteSpace(tag.Value))
                    {
                        _trace.Record(Annotations.Tag(tag.Key, tag.Value));
                    }
                }
            }
        }

        public void Dispose()
        {
            _trace?.Record(_trace.CurrentSpan.ParentSpanId == null ? Annotations.ServerSend() : Annotations.ClientRecv());
        }
    }
}
