using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using StackExchange.Redis;

namespace ATI.Services.Common.Caching.Redis.Abstractions;

public interface IRedisCache
{
    Task InitAsync();
    Task<OperationResult<byte[]>> ExecuteEvalShaAsync(byte[] scriptSha, string script, RedisKey[] keys, params RedisValue[] arguments);
    Task<OperationResult> SlaveOf(string host, int port, EndPoint master);
    Task PublishAsync(string channel, string message);
    Task<bool> TrySubscribeAsync(string channel, Action<RedisChannel, RedisValue> callback);
    Task<OperationResult> InsertAsync<T>(T redisValue, string metricEntity, TimeSpan? longRequestTime = null) where T : ICacheEntity;
    Task<OperationResult> InsertAsync<T>(T redisValue, string key, string metricEntity, TimeSpan? longRequestTime = null);
    Task<OperationResult> InsertAsync<T>(T redisValue, TimeSpan ttl, string metricEntity, TimeSpan? longRequestTime = null) where T : ICacheEntity;
    Task<OperationResult> InsertAsync<T>(T redisValue, string key, TimeSpan ttl, string metricEntity, TimeSpan? longRequestTime = null);
    Task<OperationResult<bool>> InsertIfNotExistsAsync<T>(T redisValue, string key, string metricEntity, TimeSpan? longRequestTime = null);
    Task<OperationResult<bool>> InsertIfNotExistsAsync<T>(T redisValue, string metricEntity, TimeSpan? longRequestTime = null) where T : ICacheEntity;
    Task<OperationResult> InsertManyAsync<T>(List<T> redisValue, string metricEntity, TimeSpan? longRequestTime = null) where T : ICacheEntity;
    Task InsertManyAsync<T>(Dictionary<string, T> redisValues, string metricEntity, TimeSpan? longRequestTime = null);
    Task<OperationResult<T>> GetAsync<T>(string key, string metricEntity, TimeSpan? longRequestTime = null);
    Task<OperationResult> DeleteAsync(string key, string metricEntity, TimeSpan? longRequestTime = null);
    Task<OperationResult> DeleteAsync(List<string> keys, string metricEntity, TimeSpan? longRequestTime = null);
    Task<OperationResult<List<T>>> GetManyAsync<T>(List<string> keys, string metricEntity, bool withNulls = false, TimeSpan? longRequestTime = null);
    Task<OperationResult<List<string>>> GetSetAsync(string key, string metricEntity, TimeSpan? longRequestTime = null);
    Task<OperationResult<bool>> KeyExistsAsync(string key, string metricEntity, TimeSpan? longRequestTime = null);
    Task<OperationResult> InsertIntoSetAsync(string setKey, string value, string metricEntity, TimeSpan? longRequestTime = null);
    Task<OperationResult> InsertIntoSetAsync(string setKey, List<string> values, string metricEntity, TimeSpan? longRequestTime = null);
    Task<OperationResult> InsertIntoSetsAsync(ICollection<string> setKeys, string value, string metricEntity, TimeSpan? longRequestTime = null);
    Task<OperationResult<List<string>>> GetManySetsAsync(List<string> keys, string metricEntity, bool withNulls = false, TimeSpan? longRequestTime = null);
    Task<OperationResult> InsertMultiManyAndSetAsync<T>(List<T> manyRedisValues, string setKey, string metricEntity, TimeSpan? longRequestTime = null) where T : ICacheEntity;
    Task<OperationResult<long>> IncrementAsync(string key, DateTime expireAt, string metricEntity, TimeSpan? longTimeRequest = null);
    Task<OperationResult> DeleteFromSetAsync(string setKey, string member, string metricEntity, TimeSpan? longTimeRequest = null);
    Task<OperationResult<bool>> IsMemberOfSetAsync(string setKey, string member, string metricEntity, TimeSpan? longTimeRequest = null);
    Task<OperationResult<RedisValue>> GetFromHashAsync(string hashKey, string hashField, string metricEntity, TimeSpan? longTimeRequest = null);
    Task<OperationResult> InsertIntoHashAsync(string hashKey, string hashField, RedisValue value, string metricEntity, TimeSpan? longTimeRequest = null);
    Task<OperationResult> DeleteFromHashAsync(string hashKey, string hashField, string metricEntity, TimeSpan? longTimeRequest = null);
    Task<OperationResult> ExpireAsync(string key, DateTime expiration, string metricEntity, TimeSpan? longRequestTime = null);
    Task<OperationResult> ExpireAsync(string key, TimeSpan ttl, string metricEntity, TimeSpan? longRequestTime = null);
    Task<OperationResult> SortedSetRemoveAsync(string sortedSetKey, string member, string metricEntity, TimeSpan? longTimeRequest = null);
    Task<OperationResult> SortedSetAddAsync(string sortedSetKey, string member, double valueScore, string metricEntity, TimeSpan? longTimeRequest = null);
    Task<OperationResult> InsertTypeAsHashAsync<T>(string hashKey, T data, string metricEntity, TimeSpan? longTimeRequest = null, TimeSpan? ttl = null) where T : class, new();
    Task<OperationResult> InsertManyFieldsToHashAsync<TKey, TValue>(string hashKey, List<KeyValuePair<TKey, TValue>> fieldsToInsert, string metricEntity, TimeSpan? longTimeRequest = null, TimeSpan? ttl = null);
    Task<OperationResult> InsertManyFieldsToManyHashesAsync<TKey, TValue>(Dictionary<string, List<KeyValuePair<TKey, TValue>>> valuesByHashKeys, string metricEntity, TimeSpan? longTimeRequest = null);
    Task<OperationResult> InsertHashAsync<T>(List<T> manyRedisValues, string hashKey, string metricEntity, TimeSpan? longTimeRequest = null) where T : ICacheEntity;
    Task<OperationResult<T>> GetFromHashAsync<T>(string hashKey, string hashField, string metricEntity, TimeSpan? longTimeRequest = null);
    Task<OperationResult<T>> GetTypeFromHashAsync<T>(string hashKey, string metricEntity, TimeSpan? longTimeRequest = null) where T : class, new();
    Task<OperationResult<List<KeyValuePair<TKey, TValue>>>> GetManyFieldsFromHashAsync<TKey, TValue>(string hashKey, string metricEntity, TimeSpan? longTimeRequest = null);
    Task<OperationResult<List<T>>> GetManyFromHashAsync<T>(string hashKey, string metricEntity, TimeSpan? longTimeRequest = null);
    Task<OperationResult<double>> IncreaseAsync(string key, double value, string metricEntity, TimeSpan? longTimeRequest = null);
    Task<OperationResult> InsertManyByScriptAsync<T>(List<T> redisValue, string metricEntity, TimeSpan? longRequestTime = null) where T : ICacheEntity;
    Task<OperationResult> InsertManyByScriptAsync<T>(Dictionary<string, T> redisValues, string metricEntity, TimeSpan? longRequestTime = null);
}