using System;
using ATI.Services.Common.Serializers;
using StackExchange.Redis;

namespace ATI.Services.Common.Caching.Redis
{
    public class RedisOptions
    {
        public string CacheName { get; set; }
        public TimeSpan? TimeToLive { get; set; }
        public string ConnectionString { get; set; }
        public TimeSpan RedisTimeout { get; set; }
        public bool TraceEnabled { get; set; }
        public TimeSpan CircuitBreakerSeconds { get; set; }
        public int CircuitBreakerExceptionsCount { get; set; }
        public int CacheDbNumber { get; set; }
        public TimeSpan? LongRequestTime { get; set; }
        public bool MustConnectOnInit { get; set; }
        
        public string Server { get; set; }
        public string ServiceName { get; set; }
        public int? SyncTimeoutMs { get; set; }

        public SerializerType Serializer { get; set; } = SerializerType.SystemTextJson;

        public string BuildConnectionString()
        {
            var options = ConnectionString != null
                ? ConfigurationOptions.Parse(ConnectionString)
                : new ConfigurationOptions();
            
            if (Server != null)
            {
                options.EndPoints.Clear();
                options.EndPoints.Add(Server);
            }

            if (ServiceName != null)
            {
                options.ServiceName = ServiceName;
            }

            if (SyncTimeoutMs != null)
            {
                options.SyncTimeout = SyncTimeoutMs.Value;
            }

            return options.ToString();
        }
    }
}
