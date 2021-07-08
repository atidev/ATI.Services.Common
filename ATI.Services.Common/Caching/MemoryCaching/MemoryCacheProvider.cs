using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATI.Services.Common.Caching.Redis;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using NLog;

namespace ATI.Services.Common.Caching.MemoryCaching
{
    public class MemoryCacheProvider
    {
        private readonly Dictionary<string, MemoryCacheWrapper> _caches = new Dictionary<string, MemoryCacheWrapper>();
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public MemoryCacheProvider(IOptions<MemoryCacheOptions> options, IMemoryCache memoryCache, IOptions<CacheManagerOptions> cacheManagerOptions)
        {
            var cacheOptions = cacheManagerOptions.Value.CacheOptions;
            foreach (var (cacheName, _) in cacheOptions)
            {
                options.Value.CacheOptions.TryGetValue(cacheName, out var memoryCacheOptions);
                var cacheWrapper = new MemoryCacheWrapper(
                    memoryCache,
                    memoryCacheOptions?.EntityCacheOptions ?? options.Value.DefaultOptions,
                    memoryCacheOptions?.SetsCacheOptions ?? options.Value.DefaultOptions,
                    cacheName);

                _caches.Add(cacheName, cacheWrapper);
            }
        }

        public void InitializeInvalidation(Func<LocalCacheEvent, Task> invalidationFunction)
        {
            foreach (var localCache in _caches.Values)
            {
                localCache.PublishEventAsync = invalidationFunction;
            }
        }

        public MemoryCacheWrapper GetCache(string cacheName)
        {
            if (_caches.TryGetValue(cacheName, out var existCache))
                return existCache;

            _logger.Error($"В пуле нет кеша с именем {cacheName}");
            return null;
        }
    }
}