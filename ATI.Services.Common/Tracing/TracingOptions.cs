using System;

namespace ATI.Services.Common.Tracing
{
    public class TracingOptions
    {
        public bool Enabled { get; set; }

        public float Rate { get; set; }

        public string TraceEndpoint { get; set; }

        public string ServiceName { get; set; }
        
        public TimeSpan? DefaultLongRequestTime { get; set; }
        
        /// <summary>
        /// Название сервиса для метрик, без точек и прочих символов
        /// </summary>
        public string MetricsServiceName { get; set; }
    }
}
