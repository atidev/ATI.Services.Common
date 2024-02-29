using System;
using ATI.Services.Common.Serializers;
using StackExchange.Redis;

namespace ATI.Services.Common.Caching.Redis.Abstractions;

public sealed class RedisBinarySerializer : IRedisSerializer
{
    private readonly IBinarySerializer _serializer;

    public RedisBinarySerializer(IBinarySerializer serializer)
    {
        _serializer = serializer;
    }
    
    public RedisValue Serialize<TIn>(TIn value) => _serializer.Serialize(value);

    public RedisValue Serialize(object value, Type type) => _serializer.Serialize(value, type);

    public T Deserialize<T>(RedisValue value) => _serializer.Deserialize<T>(value);
    public object Deserialize(RedisValue value, Type type) => _serializer.Deserialize(value, type);
}