using System;
using StackExchange.Redis;

namespace ATI.Services.Common.Caching.Redis.Abstractions;

public interface IRedisSerializer
{
    public RedisValue Serialize<TIn>(TIn value);
    public RedisValue Serialize(object value, Type type);

    public T Deserialize<T>(RedisValue value);
    object Deserialize(RedisValue value, Type type);
}