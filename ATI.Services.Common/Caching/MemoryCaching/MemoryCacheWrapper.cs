using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Caching.Redis;
using JetBrains.Annotations;
using Microsoft.Extensions.Caching.Memory;

namespace ATI.Services.Common.Caching.MemoryCaching
{
    public class MemoryCacheWrapper
    {
        private readonly IMemoryCache _memoryCache;
        private readonly string _instanceKey;
        private readonly MemoryCacheEntryOptions _entityCacheOptions;
        private readonly MemoryCacheEntryOptions _setsCacheOptions;
        private readonly OperationResult<List<string>> _emptyKeySetResponse = new OperationResult<List<string>>();
        private readonly OperationResult<bool> _emptyKeyOnKeyExistsResponse = new OperationResult<bool>(ActionStatus.BadRequest);
        private readonly OperationResult _emptyKeyOnInsertIntoSet = new OperationResult(ActionStatus.Ok);
        
        private bool _invalidationInitialized() => PublishEventAsync != null;
        public Func<LocalCacheEvent, Task> PublishEventAsync { get; set; }

        public MemoryCacheWrapper(
            IMemoryCache memoryCache,
            MemoryCacheEntryOptions entityCacheOptions,
            MemoryCacheEntryOptions setsCacheOptions,
            string instanceKey)
        {
            _setsCacheOptions = setsCacheOptions;
            _entityCacheOptions = entityCacheOptions;
            _instanceKey = instanceKey;
            _memoryCache = memoryCache;
        }
        
        public void InvalidateLocalCache(LocalCacheEvent cacheEvent)
        {
            switch (cacheEvent.EventType)
            {
                case LocalCacheEventType.KeyDelete:
                    _memoryCache.Remove(cacheEvent.Key);
                    break;
                case LocalCacheEventType.InsertIntoSet:
                    InsertIntoMemoryCacheSet(cacheEvent.SetKey, cacheEvent.Key);
                    break;
                case LocalCacheEventType.DeleteFromSet:
                    DeleteFromMemoryCacheSet(cacheEvent.SetKey, cacheEvent.Key);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        
        public OperationResult Insert<T>(
            T value)
            where T : ICacheEntity
        {
            if (!_invalidationInitialized())
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            _memoryCache.Set(value.GetKey(), value, _entityCacheOptions);
            
            return OperationResult.Ok;
        }
        
        public OperationResult Insert<T>(
            T value,
            string key)
        {
            if (!_invalidationInitialized())
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            _memoryCache.Set(key, value, _entityCacheOptions);
            
            return OperationResult.Ok;
        }
        
        public OperationResult InsertMany<T>(
            [NotNull] List<T> values) where T : ICacheEntity
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            if (!_invalidationInitialized())
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            foreach (var val in values)
                _memoryCache.Set(val.GetKey(), val, _entityCacheOptions);
            
            return OperationResult.Ok;
        }
        
        public OperationResult InsertMany<T>(
            Dictionary<string, T> keyValues)
        {
            if (!_invalidationInitialized())
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            if (keyValues == null || keyValues.Count == 0)
                return new OperationResult(ActionStatus.BadRequest);

            foreach (var (key, value) in keyValues)
                _memoryCache.Set(key, value, _entityCacheOptions);

            return OperationResult.Ok;
        }
        
        public OperationResult<T> Get<T>(
            string key)
        {
            if (!_invalidationInitialized())
                return new OperationResult<T>(ActionStatus.InternalOptionalServerUnavailable);

            return _memoryCache.TryGetValue<T>(key, out var result) ? 
                new OperationResult<T>(result) : 
                new OperationResult<T>(ActionStatus.NotFound);
        }
        
        public async Task<OperationResult> DeleteAsync(
            string key)
        {
            if (!_invalidationInitialized())
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            if (string.IsNullOrWhiteSpace(key))
                return OperationResult.Ok;

            await PublishKeyDeleteEventAsync(key);

            return OperationResult.Ok;
        }
        
        public async Task<OperationResult> DeleteAsync(
            List<string> keys)
        {
            if (!_invalidationInitialized())
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            if (keys == null || keys.Count == 0)
                return OperationResult.Ok;

            foreach (var key in keys)
                await PublishKeyDeleteEventAsync(key);

            return OperationResult.Ok;
        }
        
        public OperationResult<List<T>> GetMany<T>(
            List<string> keys)
        {
            if (!_invalidationInitialized())
                return new OperationResult<List<T>>(ActionStatus.InternalOptionalServerUnavailable);

            if (keys == null || keys.Count == 0)
                return new OperationResult<List<T>>(new List<T>());

            var values = new List<T>(keys.Count);
            foreach (var key in keys.Where(key => !string.IsNullOrWhiteSpace(key)))
            {
                if (_memoryCache.TryGetValue<T>(key, out var value))
                    values.Add(value);
            }

            return new OperationResult<List<T>>(values);
        }
        
        public OperationResult<List<string>> GetSet(
            string key)
        {
            if (!_invalidationInitialized())
                return new OperationResult<List<string>>(ActionStatus.InternalOptionalServerUnavailable);

            if (string.IsNullOrEmpty(key))
                return _emptyKeySetResponse;

            if (_memoryCache.TryGetValue<HashSet<string>>(key, out var value))
                return new OperationResult<List<string>>(value.ToList());
            
            return new OperationResult<List<string>>(ActionStatus.NotFound);
        }
        
        /// <summary>
        /// Возвращает два bool значения, для локального и shared кеша
        /// как правило используется перед вставкой единичного значения в сет
        /// </summary>
        public OperationResult<bool> KeyExists(
            string key)
        {
            if (!_invalidationInitialized())
                return new OperationResult<bool>(ActionStatus.InternalOptionalServerUnavailable);

            if (string.IsNullOrWhiteSpace(key))
                return _emptyKeyOnKeyExistsResponse;
            
            return new OperationResult<bool>(_memoryCache.TryGetValue(key, out _));
        }
        
        public async Task<OperationResult> InsertIntoSetAsync(
            string setKey,
            string value)
        {
            if (!_invalidationInitialized())
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            if (string.IsNullOrWhiteSpace(setKey) || string.IsNullOrWhiteSpace(value))
                return _emptyKeyOnInsertIntoSet;

            await PublishSetEventAsync(setKey, value, LocalCacheEventType.InsertIntoSet);

            return OperationResult.Ok;
        }
        
        public async Task<OperationResult> InsertIntoSetIfExistsAsync(
            string setKey,
            string value)
        {
            if (!_invalidationInitialized())
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            if (string.IsNullOrWhiteSpace(setKey))
                return _emptyKeyOnInsertIntoSet;

            await PublishSetEventAsync(setKey, value, LocalCacheEventType.InsertIntoSet);
            
            return new OperationResult(ActionStatus.Ok);
        }
        
        public OperationResult InsertMultiManyAndSet<T>(
            List<T> manyCacheEntities,
            string setKey) where T : ICacheEntity
        {
            if (!_invalidationInitialized())
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            var keyValues = manyCacheEntities.ToDictionary(key => key.GetKey(), value => value);
            foreach (var (key, value) in keyValues)
                _memoryCache.Set(key, value, _entityCacheOptions);

            _memoryCache.Set(setKey, keyValues.Keys.ToHashSet(), _setsCacheOptions);
            
            return OperationResult.Ok;
        }
        
        public async Task<OperationResult> DeleteFromSetAsync(
            string setKey,
            string entityKey)
        {
            if (!_invalidationInitialized())
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            await PublishSetEventAsync(setKey, entityKey, LocalCacheEventType.DeleteFromSet);

            return OperationResult.Ok;
        }
        
        public OperationResult<bool> IsMemberOfSet(
            string setKey,
            string entityKey)
        {
            if (!_invalidationInitialized())
                return new OperationResult<bool>(ActionStatus.InternalOptionalServerUnavailable);

            if (_memoryCache.TryGetValue<HashSet<string>>(setKey, out var localSet))
                return localSet.Count == 0
                    ? new OperationResult<bool>(false)
                    : new OperationResult<bool>(localSet.Contains(entityKey));

            return new OperationResult<bool>(ActionStatus.NotFound);
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