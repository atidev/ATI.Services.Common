using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NLog;

namespace ATI.Services.Common.Caching.Redis
{
    public class RedisProvider
    {
        private readonly Dictionary<string, RedisCache> _redisCaches = new();
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public RedisProvider(IOptions<CacheManagerOptions> cacheManagerOptions)
        {
            var cacheOptions = cacheManagerOptions.Value.CacheOptions;
            var manager = new CacheHitRatioManager(cacheManagerOptions.Value.HitRatioManagerUpdatePeriod);
            manager.Start();
            foreach (var kvDataBaseOptions in cacheOptions)
            {
                var cache = new RedisCache(kvDataBaseOptions.Value, manager);
                _redisCaches.Add(kvDataBaseOptions.Key, cache);
            } 
        }

        public RedisCache GetCache(string cacheName)
        {   
            var isDbConfigured = _redisCaches.TryGetValue(cacheName, out var cache);
            if (isDbConfigured)
            {
                return cache;
            }
            _logger.Error($"В пуле нет базы {cacheName}");
            return null;
        }
        
        public List<RedisCache> GetAllCaches() => _redisCaches.Values.ToList();

        public async Task InitAsync()
        { 
            foreach (var cache in _redisCaches.Values)
            {
                await cache.InitAsync();
            }
        }
    }
}
