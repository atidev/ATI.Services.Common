using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.Caching.Redis.Abstractions;
using ATI.Services.Common.Serializers;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NLog;

namespace ATI.Services.Common.Caching.Redis;

public class RedisProvider
{
    private readonly Dictionary<string, RedisCache> _redisCaches = new();
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public RedisProvider(IOptions<CacheManagerOptions> cacheManagerOptions, SerializerProvider serializerProvider)
    {
        var cacheOptions = cacheManagerOptions.Value.CacheOptions;
        var manager = new CacheHitRatioManager(cacheManagerOptions.Value.HitRatioManagerUpdatePeriod);
        manager.Start();

        foreach (var (redisName, options) in cacheOptions)
        {
            IRedisSerializer serializer = serializerProvider.GetSerializerByType(options.Serializer) switch
            {
                null => serializerProvider.GetBinarySerializerByType(options.Serializer) switch
                {
                    null => throw new ArgumentOutOfRangeException($"Не найден сериализатор для типа {options.Serializer} для базы {redisName}"),
                    { } binarySerializer => new RedisBinarySerializer(binarySerializer)
                },
                { } jsonSerializer => new RedisStringSerializer(jsonSerializer)
            };
            
            var cache = new RedisCache(options, manager, serializer);
            _redisCaches.Add(redisName, cache);
        }
    }

    [PublicAPI]
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

    [PublicAPI]
    public List<RedisCache> GetAllCaches() => _redisCaches.Values.ToList();

    public async Task InitAsync()
    {
        foreach (var cache in _redisCaches.Values)
        {
            await cache.InitAsync();
        }
    }
}