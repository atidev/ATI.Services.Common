using System;

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
    }
}
