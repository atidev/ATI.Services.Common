using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Caching.Redis;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Polly;

namespace ATI.Services.Common.Caching.MemoryCaching
{
    public class TwoLevelCache
    {
        private readonly RedisCache _redisCache;
        private readonly IMemoryCache _memoryCache;
        private readonly string _instanceKey;
        private readonly MemoryCacheEntryOptions _entityCacheOptions;
        private readonly MemoryCacheEntryOptions _setsCacheOptions;
        private readonly OperationResult<List<string>> _emptyKeySetResponse = new OperationResult<List<string>>();
        private readonly OperationResult<KeyExistResult> _emptyKeyOnKeyExistsResponse = new OperationResult<KeyExistResult>(ActionStatus.BadRequest);
        private readonly OperationResult _emptyKeyOnInsertIntoSet = new OperationResult(ActionStatus.Ok);
        public Func<LocalCacheEvent, Task> PublishEventAsync { get; set; }
        private bool _invalidationInitialized;
        private static readonly Policy<bool> InitPolicy =
            Policy<bool>
                .Handle<Exception>()
                .OrResult(res => !res)
                .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(30));

        public TwoLevelCache(
            RedisCache redisCache,
            IMemoryCache memoryCache,
            string instanceKey,
            MemoryCacheEntryOptions entityCacheOptions,
            MemoryCacheEntryOptions setsCacheOptions)
        {
            _instanceKey = instanceKey;
            _setsCacheOptions = setsCacheOptions;
            _entityCacheOptions = entityCacheOptions;
            _memoryCache = memoryCache;
            _redisCache = redisCache;
        }

        public async Task LocalCacheInvalidationSubscribeAsync()
        {
            await InitPolicy.ExecuteAsync(async () => await _redisCache.TrySubscribeAsync(_instanceKey, (channel, value) =>
            {
                var redisEvent = JsonConvert.DeserializeObject<LocalCacheEvent>(value);
                switch (redisEvent.EventType)
                {
                    case LocalCacheEventType.KeyDelete:
                        _memoryCache.Remove(redisEvent.Key);
                        break;
                    case LocalCacheEventType.InsertIntoSet:
                        InsertIntoMemoryCacheSet(redisEvent.SetKey, redisEvent.Key);
                        break;
                    case LocalCacheEventType.DeleteFromSet:
                        DeleteFromMemoryCacheSet(redisEvent.SetKey, redisEvent.Key);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }));

            _invalidationInitialized = true;
        }

        public async Task<OperationResult> InsertAsync<T>(
            T value,
            string metricEntity,
            TimeSpan? longRequestTime = null)
            where T : ICacheEntity
        {
            if (!_invalidationInitialized)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            _memoryCache.Set(value.GetKey(), value, _entityCacheOptions);

            return await _redisCache.InsertAsync(value, metricEntity, longRequestTime);
        }

        public async Task<OperationResult> InsertAsync<T>(
            T value,
            string key,
            string metricEntity,
            TimeSpan? longRequestTime = null)
        {
            if (!_invalidationInitialized)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            _memoryCache.Set(key, value, _entityCacheOptions);

            return await _redisCache.InsertAsync(value, key, metricEntity, longRequestTime);
        }

        public async Task<OperationResult> InsertManyAsync<T>(
            [NotNull] List<T> values,
            string metricEntity,
            TimeSpan? longRequestTime = null) where T : ICacheEntity
        {
            if (!_invalidationInitialized)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            foreach (var val in values)
                _memoryCache.Set(val.GetKey(), val, _entityCacheOptions);

            return await _redisCache.InsertManyAsync(values, metricEntity, longRequestTime);
        }

        public async Task InsertManyAsync<T>(
            Dictionary<string, T> keyValues,
            string metricEntity,
            TimeSpan? longRequestTime = null)
        {
            if (!_invalidationInitialized)
                return;

            if (keyValues == null || keyValues.Count == 0)
                return;

            foreach (var (key, value) in keyValues)
                _memoryCache.Set(key, value, _entityCacheOptions);

            await _redisCache.InsertManyAsync(keyValues, metricEntity, longRequestTime);
        }

        public async Task<OperationResult<T>> GetAsync<T>(
            string key,
            string metricEntity,
            TimeSpan? longRequestTime = null)
        {
            if (!_invalidationInitialized)
                return new OperationResult<T>(ActionStatus.InternalOptionalServerUnavailable);

            if (_memoryCache.TryGetValue<T>(key, out var result))
                return new OperationResult<T>(result);

            var fromRedisOperation = await _redisCache.GetAsync<T>(key, metricEntity, longRequestTime);
            if (!fromRedisOperation.Success)
                return fromRedisOperation;

            _memoryCache.Set(key, fromRedisOperation.Value, _entityCacheOptions);
            return fromRedisOperation;
        }

        public async Task<OperationResult> DeleteAsync(
            string key,
            string metricEntity,
            TimeSpan? longRequestTime = null)
        {
            if (!_invalidationInitialized)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            if (string.IsNullOrWhiteSpace(key))
                return new OperationResult(ActionStatus.Ok);

            var deleteRedisOperation = await _redisCache.DeleteAsync(key, metricEntity, longRequestTime);
            if (!deleteRedisOperation.Success)
                return deleteRedisOperation;

            await PublishKeyDeleteEventAsync(key);

            return deleteRedisOperation;
        }

        public async Task<OperationResult> DeleteAsync(
            List<string> keys,
            string metricEntity,
            TimeSpan? longRequestTime = null)
        {
            if (!_invalidationInitialized)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            if (keys == null || keys.Count == 0)
                return new OperationResult(ActionStatus.Ok);

            var deleteRedisOperation = await _redisCache.DeleteAsync(keys, metricEntity, longRequestTime);
            if (!deleteRedisOperation.Success)
                return deleteRedisOperation;

            foreach (var key in keys)
                await PublishKeyDeleteEventAsync(key);

            return deleteRedisOperation;
        }

        public async Task<OperationResult<List<T>>> GetManyAsync<T>(
            List<string> keys,
            string metricEntity,
            bool withNulls = false,
            TimeSpan? longRequestTime = null)
        {
            if (!_invalidationInitialized)
                return new OperationResult<List<T>>(ActionStatus.InternalOptionalServerUnavailable);

            if (keys == null || keys.Count == 0)
                return new OperationResult<List<T>>(); //в импл. redis - так, мб стоит new List<T> ?

            var values = new List<T>(keys.Count);
            var localMisses = new List<string>(keys.Count);
            foreach (var key in keys.Where(key => !string.IsNullOrWhiteSpace(key)))
            {
                if (_memoryCache.TryGetValue<T>(key, out var value))
                    values.Add(value);
                else
                    localMisses.Add(key);
            }

            if (values.Count == keys.Count(key => !string.IsNullOrWhiteSpace(key))) //ha-ha-ha
                return new OperationResult<List<T>>(values);

            var fromRedisOperation = await _redisCache.GetManyAsync<T>(localMisses, metricEntity, withNulls, longRequestTime);
            if (!fromRedisOperation.Success)
                return fromRedisOperation;

            if (fromRedisOperation.Value.Count != 0)
            {
                foreach (var redisHit in fromRedisOperation.Value)
                    if (redisHit is ICacheEntity cacheEntity)
                        _memoryCache.Set(cacheEntity.GetKey(), cacheEntity, _entityCacheOptions);

                values.AddRange(fromRedisOperation.Value);
            }

            return new OperationResult<List<T>>(values);
        }

        public async Task<OperationResult<List<string>>> GetSetAsync(
            string key,
            string metricEntity,
            TimeSpan? longRequestTime = null)
        {
            if (!_invalidationInitialized)
                return new OperationResult<List<string>>(ActionStatus.InternalOptionalServerUnavailable);

            if (string.IsNullOrEmpty(key))
                return _emptyKeySetResponse;

            if (_memoryCache.TryGetValue<HashSet<string>>(key, out var value))
                return new OperationResult<List<string>>(value.ToList());

            var fromRedisOperation = await _redisCache.GetSetAsync(key, metricEntity, longRequestTime);
            if (!fromRedisOperation.Success)
                return fromRedisOperation;

            _memoryCache.Set(key, fromRedisOperation.Value.ToHashSet(), _setsCacheOptions);
            return fromRedisOperation;
        }


        /// <summary>
        /// Возвращает два bool значения, для локального и shared кеша
        /// как правило используется перед вставкой единичного значения в сет
        /// </summary>
        public async Task<OperationResult<KeyExistResult>> KeyExistsAsync(
            string key,
            string metricEntity,
            TimeSpan? longRequestTime = null)
        {
            if (!_invalidationInitialized)
                return new OperationResult<KeyExistResult>(ActionStatus.InternalOptionalServerUnavailable);

            var keyExistResult = new KeyExistResult();

            if (string.IsNullOrWhiteSpace(key))
                return _emptyKeyOnKeyExistsResponse;

            keyExistResult.LocalKeyExist = _memoryCache.TryGetValue(key, out _);

            var redisKeyExistsOperation = await _redisCache.KeyExistsAsync(key, metricEntity, longRequestTime);
            if (!redisKeyExistsOperation.Success)
                return new OperationResult<KeyExistResult>(redisKeyExistsOperation);

            keyExistResult.SharedKeyExist = redisKeyExistsOperation.Value;
            return new OperationResult<KeyExistResult>(keyExistResult);
        }

        public async Task<OperationResult> InsertIntoSetAsync(
            string setKey,
            string value,
            string metricEntity,
            TimeSpan? longRequestTime = null)
        {
            if (!_invalidationInitialized)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            if (string.IsNullOrWhiteSpace(setKey) || string.IsNullOrWhiteSpace(value))
                return _emptyKeyOnInsertIntoSet;

            var insertIntoSetRedisOperation = await _redisCache.InsertIntoSetAsync(setKey, value, metricEntity, longRequestTime);
            if (!insertIntoSetRedisOperation.Success)
                return insertIntoSetRedisOperation;

            await PublishSetEventAsync(setKey, value, LocalCacheEventType.InsertIntoSet);

            return insertIntoSetRedisOperation;
        }
        
        public async Task<OperationResult> InsertIntoSetIfExistsAsync(
            string setKey,
            string value,
            string metricEntity,
            TimeSpan? longRequestTime = null)
        {
            if (!_invalidationInitialized)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            if (string.IsNullOrWhiteSpace(setKey))
                return _emptyKeyOnInsertIntoSet;

            var sharedKeyExist = await _redisCache.KeyExistsAsync(setKey, metricEntity, longRequestTime);
            if (sharedKeyExist.Success && sharedKeyExist.Value)
                await _redisCache.InsertIntoSetAsync(setKey, value, metricEntity, longRequestTime);

            await PublishSetEventAsync(setKey, value, LocalCacheEventType.InsertIntoSet);
            
            return new OperationResult(ActionStatus.Ok);
        }

        public async Task<OperationResult> InsertMultiManyAndSetAsync<T>(
            List<T> manyCacheEntities,
            string setKey,
            string metricEntity,
            TimeSpan? longRequestTime = null) where T : ICacheEntity
        {
            if (!_invalidationInitialized)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            var keyValues = manyCacheEntities.ToDictionary(key => key.GetKey(), value => value);
            foreach (var (key, value) in keyValues)
                _memoryCache.Set(key, value, _entityCacheOptions);

            _memoryCache.Set(setKey, keyValues.Keys.ToHashSet(), _setsCacheOptions);
            
            return await _redisCache.InsertMultiManyAndSetAsync(manyCacheEntities, setKey, metricEntity, longRequestTime);
        }

        public async Task<OperationResult> DeleteFromSetAsync(
            string setKey,
            string entityKey,
            string metricEntity,
            TimeSpan? longRequestTime = null)
        {
            if (!_invalidationInitialized)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            var redisDeleteFromSetOperation = await _redisCache.DeleteFromSetAsync(setKey, entityKey, metricEntity, longRequestTime);
            if (!redisDeleteFromSetOperation.Success)
                return redisDeleteFromSetOperation;

            await PublishSetEventAsync(setKey, entityKey, LocalCacheEventType.DeleteFromSet);

            return redisDeleteFromSetOperation;
        }

        public async Task<OperationResult<bool>> IsMemberOfSetAsync(
            string setKey,
            string entityKey,
            string metricEntity,
            TimeSpan? longRequestTime = null)
        {
            if (!_invalidationInitialized)
                return new OperationResult<bool>(ActionStatus.InternalOptionalServerUnavailable);

            if (_memoryCache.TryGetValue<HashSet<string>>(setKey, out var localSet))
                return localSet.Count == 0
                    ? new OperationResult<bool>(false)
                    : new OperationResult<bool>(localSet.Contains(entityKey));

            return await _redisCache.IsMemberOfSetAsync(setKey, entityKey, metricEntity, longRequestTime);
        }

        private void DeleteFromMemoryCacheSet(string setKey, string key)
        {
            if (string.IsNullOrWhiteSpace(setKey) || string.IsNullOrWhiteSpace(key))
                return;
            
            if (_memoryCache.TryGetValue<HashSet<string>>(setKey, out var localSet))
                if (localSet.Count != 0)
                    localSet.Remove(key);
        }

        private void InsertIntoMemoryCacheSet(string setKey, string key)
        {
            if (string.IsNullOrWhiteSpace(setKey) || string.IsNullOrWhiteSpace(key))
                return;
            
            if (_memoryCache.TryGetValue<HashSet<string>>(setKey, out var set))
                set.Add(key);
        }

        private async Task PublishKeyDeleteEventAsync(string key)
        {
            var payload = new LocalCacheEvent
            {
                InstanceKey = _instanceKey,
                Key = key,
                EventType = LocalCacheEventType.KeyDelete
            };
            
            await PublishEventAsync(payload);
        }

        private async Task PublishSetEventAsync(string setKey, string key, LocalCacheEventType eventType)
        {
            if (string.IsNullOrWhiteSpace(setKey) || string.IsNullOrWhiteSpace(key))
                return;
            
            var payload = new LocalCacheEvent
            {
                InstanceKey = _instanceKey,
                Key = key,
                SetKey = setKey,
                EventType = eventType
            };
            
            await PublishEventAsync(payload);
        }
    }
}