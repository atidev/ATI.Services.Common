using System;
using System.Collections.Generic;

namespace ATI.Services.Common.Caching.Redis
{
    public class CacheManagerOptions
    {
        public Dictionary<string, RedisOptions> CacheOptions { get; set; }
        public TimeSpan HitRatioManagerUpdatePeriod { get; set; }
    }
}
