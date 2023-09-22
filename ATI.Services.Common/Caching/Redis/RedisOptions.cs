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
        
        /// <summary>
        /// Хосты. Если их несколько, перечислять через запятую
        /// </summary>
        public string Servers { get; set; }
        
        /// <summary>
        /// Имя redis в Sentinel
        /// </summary>
        public string ServiceName { get; set; }
        
        /// <summary>
        /// Таймаут к операциям редиса.
        /// Лучше указывать его меньше, чем RedisTimeout - в этом случае в логи падают развернутые ошибки от Redis, что пошло не так
        /// </summary>
        public int? SyncTimeoutMs { get; set; }

        public SerializerType Serializer { get; set; } = SerializerType.SystemTextJson;

        public string BuildConnectionString()
        {
            var options = ConnectionString != null
                ? ConfigurationOptions.Parse(ConnectionString)
                : new ConfigurationOptions();
            
            if (Servers != null)
            {
                options.EndPoints.Clear();
                options.EndPoints.Add(Servers);
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
