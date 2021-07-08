using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.Caching.Redis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NLog;

namespace ATI.Services.Common.Caching.MemoryCaching
{
    
    public class TwoLevelCacheProvider
    {
        private readonly Dictionary<string, TwoLevelCache> _caches = new Dictionary<string, TwoLevelCache>();
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public TwoLevelCacheProvider(IOptions<MemoryCacheOptions> options, RedisProvider redisProvider, IMemoryCache memoryCache, IOptions<CacheManagerOptions> cacheManagerOptions)
        {
            var cacheOptions = cacheManagerOptions.Value.CacheOptions;
            foreach (var (cacheName, _) in cacheOptions)
            {
                var redisCache = redisProvider.GetCache(cacheName);
                options.Value.CacheOptions.TryGetValue(cacheName, out var memoryCacheOptions);
                var cacheWrapper = new TwoLevelCache(
                    redisCache,
                    memoryCache,
                    cacheName,
                    memoryCacheOptions?.EntityCacheOptions ?? options.Value.DefaultOptions,
                    memoryCacheOptions?.SetsCacheOptions ?? options.Value.DefaultOptions);

                _caches.Add(cacheName, cacheWrapper);
            }
        }

        public async Task InitAsync()
        {
            await Task.WhenAll(_caches.Select(cache => cache.Value.LocalCacheInvalidationSubscribeAsync()));
        }

        public TwoLevelCache GetCache(string cacheName)
        {
            if (_caches.TryGetValue(cacheName, out var existCache))
                return existCache;

            _logger.Error($"В пуле нет кеша с именем {cacheName}");
            return null;
        }
    }
}