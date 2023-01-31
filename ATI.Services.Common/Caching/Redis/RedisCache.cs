using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Extensions;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Metrics;
using JetBrains.Annotations;
using NLog;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using StackExchange.Redis;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using ATI.Services.Common.Serializers;
using Polly.Retry;
using Polly.Wrap;

namespace ATI.Services.Common.Caching.Redis
{
    [PublicAPI]
    public class RedisCache : BaseRedisCache
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private IDatabase _redisDb;

        private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
        private readonly AsyncPolicyWrap _policy;
        private readonly HitRatioCounter _counter;
        private readonly MetricsTracingFactory _metricsTracingFactory;
        private bool _connected;

        private RedisScriptCache _redisScriptCache;

        private static readonly AsyncRetryPolicy<ConnectionMultiplexer> InitForeverPolicy =
            Policy<ConnectionMultiplexer>
                .Handle<Exception>()
                .OrResult(res => res == null)
                .WaitAndRetryForeverAsync(_ => TimeSpan.FromSeconds(30));
        
        private static readonly AsyncRetryPolicy<ConnectionMultiplexer> InitThreeTimesPolicy =
            Policy<ConnectionMultiplexer>
                .Handle<Exception>()
                .OrResult(res => res == null)
                .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(5));


        public RedisCache(RedisOptions options, CacheHitRatioManager manager) 
            : base(SerializerFactory.GetSerializerByType(options.Serializer))
        {
            Options = options;
            _metricsTracingFactory = MetricsTracingFactory.CreateRedisMetricsFactory(nameof(RedisCache), Options.LongRequestTime);
            _circuitBreakerPolicy = Policy.Handle<Exception>()
                .CircuitBreakerAsync(Options.CircuitBreakerExceptionsCount, Options.CircuitBreakerSeconds);
            _policy = Policy.WrapAsync(Policy.TimeoutAsync(Options.RedisTimeout, TimeoutStrategy.Pessimistic), _circuitBreakerPolicy);
            _counter = manager.CreateCounter(nameof(RedisCache));
        }

        public async Task InitAsync()
        {
            if (Options.MustConnectOnInit)
                await ConnectToRedisAsync(true);
            else
                ConnectToRedisAsync(false).Forget();
        }

        private async Task ConnectToRedisAsync(bool mustConnectOnInit)
        {
            try
            {
                var policy = mustConnectOnInit ? InitThreeTimesPolicy : InitForeverPolicy;
                var connectionMultiplexer = await policy.ExecuteAsync(async () =>
                {
                    try
                    {
                        return await ConnectionMultiplexer.ConnectAsync(Options.ConnectionString);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Redis connection error in init retry policy");
                        throw;
                    }
                });
                _redisDb = connectionMultiplexer.GetDatabase(Options.CacheDbNumber);
                _redisScriptCache = new RedisScriptCache(_redisDb, Options, _metricsTracingFactory, _circuitBreakerPolicy, _policy);
                _connected = true;
            }
            catch (Exception e)
            {
                _logger.ErrorWithObject(e, "Redis connection error");
                throw;
            }
        }

        public async Task<OperationResult<byte[]>> ExecuteEvalShaAsync(
            byte[] scriptSha,
            string script,
            RedisKey[] keys,
            params RedisValue[] arguments)
        {
            if (!_connected)
                return new OperationResult<byte[]>(ActionStatus.InternalOptionalServerUnavailable);

            if (scriptSha != null)
            {
                var evalShaOperation = await ExecuteAsync(
                    async () => await _redisDb.ScriptEvaluateAsync(scriptSha, keys, arguments),
                    new
                    {
                        scriptSha,
                        script,
                        keys,
                        arguments
                    });
                if (evalShaOperation.Success)
                    return new OperationResult<byte[]>(scriptSha);
            }

            var evalOperation = await ExecuteAsync(
                async () => await _redisDb.ScriptEvaluateAsync(script, keys, arguments), new
                {
                    script,
                    keys,
                    arguments
                });

            return evalOperation.Success
                ? new OperationResult<byte[]>(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(script)))
                : new OperationResult<byte[]>(evalOperation);
        }

        public async Task<OperationResult> SlaveOf(string host, int port, EndPoint master)
        {
            try
            {
                await _redisDb.Multiplexer.GetServer(host, port).ReplicaOfAsync(master);
                return OperationResult.Ok;
            }
            catch (Exception e)
            {
                _logger.ErrorWithObject(e, new {host, port, master});
                return new OperationResult(e);
            }
        }

        public async Task PublishAsync(string channel, string message)
        {
            if (!_connected)
                return;

            await _redisDb
                .Multiplexer
                .GetSubscriber()
                .PublishAsync(channel, message);
        }

        public async Task<bool> TrySubscribeAsync(string channel, Action<RedisChannel, RedisValue> callback)
        {
            if (!_connected)
                return false;

            await _redisDb
                .Multiplexer
                .GetSubscriber()
                .SubscribeAsync(channel, callback);

            return true;
        }

        public async Task<OperationResult> InsertAsync<T>(T redisValue, string metricEntity, TimeSpan? longRequestTime = null) where T : ICacheEntity
            => 
                await InsertPrivateAsync(redisValue, redisValue.GetKey(), Options.TimeToLive, metricEntity, longRequestTime);

        public async Task<OperationResult> InsertAsync<T>(T redisValue, string key, string metricEntity, TimeSpan? longRequestTime = null)
            =>
                await InsertPrivateAsync(redisValue, key, Options.TimeToLive, metricEntity, longRequestTime);

        public async Task<OperationResult> InsertAsync<T>(T redisValue, TimeSpan ttl, string metricEntity, TimeSpan? longRequestTime = null) where T : ICacheEntity
            =>
                await InsertPrivateAsync(redisValue, redisValue.GetKey(), ttl, metricEntity, longRequestTime);

        public async Task<OperationResult> InsertAsync<T>(T redisValue, string key, TimeSpan ttl, string metricEntity, TimeSpan? longRequestTime = null)
            =>
                await InsertPrivateAsync(redisValue, key, ttl, metricEntity, longRequestTime);

        public async Task<OperationResult<bool>> InsertIfNotExistsAsync<T>(T redisValue, string key, string metricEntity, TimeSpan? longRequestTime = null)
            =>
                await InsertPrivateAsync(redisValue, key, Options.TimeToLive, metricEntity, longRequestTime, When.NotExists);

        public async Task<OperationResult<bool>> InsertIfNotExistsAsync<T>(T redisValue, string metricEntity, TimeSpan? longRequestTime = null) where T : ICacheEntity
            =>
                await InsertPrivateAsync(redisValue, redisValue.GetKey(), Options.TimeToLive, metricEntity, longRequestTime, When.NotExists);

        public async Task<OperationResult> InsertManyAsync<T>([NotNull] List<T> redisValue, string metricEntity, TimeSpan? longRequestTime = null) where T : ICacheEntity
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (redisValue.Count < 0)
                return OperationResult.Ok;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(redisValue.FirstOrDefault()?.GetKey()), metricEntity, requestParams: new { RedisValues = redisValue }, longRequestTime: longRequestTime))
            {
                var tasks = new List<Task>(redisValue.Select(async cacheEntity =>
                    await _redisDb.StringSetAsync(cacheEntity.GetKey(), Serializer.Serialize(cacheEntity), Options.TimeToLive)));

                var result = await ExecuteAsync(async () => await Task.WhenAll(tasks), redisValue);

                return result;
            }
        }

        public async Task InsertManyAsync<T>(Dictionary<string, T> redisValues, string metricEntity, TimeSpan? longRequestTime = null)
        {
            if (redisValues == null || redisValues.Count == 0 || !_connected)
                return;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(redisValues.FirstOrDefault().Key), metricEntity, requestParams: new { RedisValues = redisValues }, longRequestTime: longRequestTime))
            {
                var tasks =
                    new List<Task>(
                        redisValues.Select(async value => await ExecuteAsync(
                            async () =>
                                await _redisDb.StringSetAsync(value.Key,
                                    Serializer.Serialize(value.Value), Options.TimeToLive), redisValues)
                        ));
                await Task.WhenAll(tasks);
            }
        }


        public async Task<OperationResult<T>> GetAsync<T>(string key, string metricEntity, TimeSpan? longRequestTime = null)
        {
            if (!_connected)
                return new OperationResult<T>(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(key))
                return new OperationResult<T>();

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(key), metricEntity, requestParams: new { RedisKey = key }, longRequestTime: longRequestTime))
            {
                var operationResult = await ExecuteAsync(async () => await _redisDb.StringGetAsync(key), key);

                if (!operationResult.Success)
                {
                    _counter.Miss();

                    return new OperationResult<T>(operationResult.ActionStatus);
                }

                _counter.Hit();
                var value = Serializer.Deserialize<T>(operationResult.Value);
                return new OperationResult<T>(value);
            }
        }

        public async Task<OperationResult> DeleteAsync(string key, string metricEntity, TimeSpan? longRequestTime = null)
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(key))
                return OperationResult.Ok;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(key), metricEntity, requestParams: new { RedisKey = key }, longRequestTime: longRequestTime))
            {
                return await ExecuteAsync(async () => await _redisDb.KeyDeleteAsync(key), key);
            }
        }

        public async Task<OperationResult> DeleteAsync(List<string> keys, string metricEntity, TimeSpan? longRequestTime = null)
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (keys == null || keys.Count == 0)
                return OperationResult.Ok;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(keys.FirstOrDefault()), metricEntity, requestParams: new { RedisKeys = keys }, longRequestTime: longRequestTime))
            {
                return await ExecuteAsync(async () =>
                {
                    foreach (var key in keys)
                    {
                        await _redisDb.KeyDeleteAsync(key);
                    }
                }, keys);
            }
        }

        public async Task<OperationResult<List<T>>> GetManyAsync<T>(List<string> keys, string metricEntity, bool withNulls = false, TimeSpan? longRequestTime = null)
        {
            if (!_connected)
                return new OperationResult<List<T>>(ActionStatus.InternalOptionalServerUnavailable);
            if (keys == null || keys.Count == 0)
                return new OperationResult<List<T>>();

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(keys.FirstOrDefault()), metricEntity, requestParams: new { RedisKeys = keys, WithNulls = withNulls }, longRequestTime: longRequestTime))
            {
                var keysArray = keys.Select(key => (RedisKey)key).ToArray();
                var operationResult = await ExecuteAsync(async () => await _redisDb.StringGetAsync(keysArray), keys);
                if (!operationResult.Success)
                    return new OperationResult<List<T>>(operationResult);

                var result = withNulls
                    ? operationResult.Value.Select(value => value.HasValue ? Serializer.Deserialize<T>(value) : default).ToList()
                    : operationResult.Value.Where(value => value.HasValue).Select(value => Serializer.Deserialize<T>(value)).ToList();

                var amountOfFoundValues = operationResult.Value.Count(value => value.HasValue);
                _counter.Hit(amountOfFoundValues);
                _counter.Miss(keysArray.Length - amountOfFoundValues);

                return new OperationResult<List<T>>(result);
            }
        }

        public async Task<OperationResult<List<string>>> GetSetAsync(string key, string metricEntity, TimeSpan? longRequestTime = null)
        {
            if (!_connected)
                return new OperationResult<List<string>>(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(key))
                return new OperationResult<List<string>>();

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(key), metricEntity, requestParams: new { Key = key }, longRequestTime: longRequestTime))
            {
                var operationResult = await ExecuteAsync(async () => await _redisDb.SetMembersAsync(key), key);

                if (operationResult.Success)
                {
                    if (operationResult.Value.Length == 0)
                    {
                        return new OperationResult<List<string>>(ActionStatus.NotFound);
                    }

                    if (operationResult.Value.Length == 1 && string.IsNullOrEmpty(operationResult.Value.FirstOrDefault()))
                    {
                        return new OperationResult<List<string>>(new List<string>());
                    }

                    var result = operationResult.Value.Where(value => value.HasValue && !string.IsNullOrEmpty(value)).Select(value => value.ToString()).ToList();

                    return new OperationResult<List<string>>(result);
                }

                return new OperationResult<List<string>>(operationResult.ActionStatus);
            }
        }

        public async Task<OperationResult<bool>> KeyExistsAsync(string key, string metricEntity, TimeSpan? longRequestTime = null)
        {
            if (!_connected)
                return new OperationResult<bool>(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(key))
                return new OperationResult<bool>();

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(key), metricEntity, requestParams: new { Key = key }, longRequestTime: longRequestTime))
            {
                return await ExecuteAsync(async () => await _redisDb.KeyExistsAsync(key), key);
            }
        }

        public async Task<OperationResult> InsertIntoSetAsync(string setKey, string value, string metricEntity, TimeSpan? longRequestTime = null)
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(setKey))
                return OperationResult.Ok;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(setKey), metricEntity, requestParams: new { SetKey = setKey, Value = value }, longRequestTime: longRequestTime))
            {
                return await ExecuteAsync(async () => await _redisDb.SetAddAsync(setKey, value), new { setKey, value });
            }
        }

        public async Task<OperationResult> InsertIntoSetAsync(string setKey, List<string> values, string metricEntity, TimeSpan? longRequestTime = null)
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(setKey) || values.Count == 0)
                return new OperationResult(ActionStatus.Ok);

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(setKey), metricEntity,
                requestParams: new {SetKey = setKey, Values = values}, longRequestTime: longRequestTime))
            {
                return await ExecuteAsync(async () => await _redisDb.SetAddAsync(setKey, values.Cast<RedisValue>().ToArray()), new { setKey, values });
            }
        }

        public async Task<OperationResult> InsertIntoSetsAsync(ICollection<string> setKeys, string value, string metricEntity, TimeSpan? longRequestTime = null)
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (setKeys == null || setKeys.Count == 0)
                return OperationResult.Ok;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(setKeys.FirstOrDefault()), metricEntity, requestParams: new { SetsKeys = setKeys, Value = value }, longRequestTime: longRequestTime))
            {
                var setAddTasks = setKeys.Select(async setKey => await InsertEntityToSetWithPolicy(setKey, value));
                return await ExecuteAsync(async () => await Task.WhenAll(setAddTasks), new { setKeys, value });
            }

            async Task<OperationResult<bool>> InsertEntityToSetWithPolicy(string key, string val) => await ExecuteAsync(async () => await _redisDb.SetAddAsync(key, val), new { setKeys, value });
        }

        public async Task<OperationResult<List<string>>> GetManySetsAsync(List<string> keys, string metricEntity, bool withNulls = false, TimeSpan? longRequestTime = null)
        {
            if (!_connected)
                return new OperationResult<List<string>>(ActionStatus.InternalOptionalServerUnavailable);
            if (keys == null || keys.Count == 0)
                return new OperationResult<List<string>>();

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(keys.FirstOrDefault()), metricEntity, requestParams: new { RedisKeys = keys, WithNulls = withNulls }, longRequestTime: longRequestTime))
            {
                var transaction = _redisDb.CreateTransaction();
                var operations = keys.Select(key =>
                    new Func<ITransaction, Task<RedisValue[]>>(tran => tran.SetMembersAsync(key)));
                var responses = operations.Select(command => command(transaction)).ToList();

                var operationResult = await ExecuteAsync(async () => await transaction.ExecuteAsync(), keys);
                if (!operationResult.Success)
                    return new OperationResult<List<string>>(operationResult);

                var results = new List<string>();
                foreach (var response in responses)
                {
                    var result = withNulls
                        ? response.Result.Select(value => value.HasValue ? value.ToString() : default).ToList()
                        : response.Result.Where(value => value.HasValue).Select(value => value.ToString()).ToList();

                    results.AddRange(result);
                }

                return new OperationResult<List<string>>(results);
            }
        }

        public async Task<OperationResult> InsertMultiManyAndSetAsync<T>(List<T> manyRedisValues, string setKey, string metricEntity, TimeSpan? longRequestTime = null) where T : ICacheEntity
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(setKey))
                return OperationResult.Ok;

            if (manyRedisValues.Count == 0)
                return await ExecuteAsync(async () => await _redisDb.SetAddAsync(setKey, ""), new { manyRedisValues, setKey });

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(setKey), metricEntity, requestParams: new { RedisValues = manyRedisValues, SetKey = setKey }, longRequestTime: longRequestTime))
            {
                var transaction = _redisDb.CreateTransaction();
                transaction.KeyDeleteAsync(setKey).Forget();
                foreach (var redisValue in manyRedisValues)
                {
                    transaction.StringSetAsync(redisValue.GetKey(), Serializer.Serialize(redisValue),
                        Options.TimeToLive).Forget();
                }
                transaction.SetAddAsync(setKey, manyRedisValues.Select(value => (RedisValue)value.GetKey()).ToArray()).Forget();
                transaction.KeyExpireAsync(setKey, Options.TimeToLive).Forget();
                return await ExecuteAsync(async () => await transaction.ExecuteAsync(), new { manyRedisValues, setKey });
            }
        }


        public async Task<OperationResult<long>> IncrementAsync(string key, DateTime expireAt, string metricEntity, TimeSpan? longTimeRequest = null)
        {
            if (!_connected)
                return new OperationResult<long>(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(key))
                return new OperationResult<long>(ActionStatus.BadRequest);

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(key), metricEntity, requestParams: new { Key = key, ExpireAt = expireAt }, longRequestTime: longTimeRequest))
            {
                var transaction = _redisDb.CreateTransaction();
                var incrementOperation = transaction.StringIncrementAsync(key);
                transaction.KeyExpireAsync(key, expireAt).Forget();
                var operation = await ExecuteAsync(async () => await transaction.ExecuteAsync(), new { key, expireAt });
                if (!operation.Success)
                    return new OperationResult<long>(operation);

                return new OperationResult<long>(incrementOperation.Result);
            }
        }

        public async Task<OperationResult> DeleteFromSetAsync(string setKey, string member, string metricEntity, TimeSpan? longTimeRequest = null)
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(setKey))
                return OperationResult.Ok;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(setKey), metricEntity, requestParams: new { SetKey = setKey, MemberOfSet = member }, longRequestTime: longTimeRequest))
            {
                return await ExecuteAsync(async () => await _redisDb.SetRemoveAsync(setKey, member), setKey);
            }
        }

        public async Task<OperationResult<bool>> IsMemberOfSetAsync(string setKey, string member, string metricEntity, TimeSpan? longTimeRequest = null)
        {
            if (!_connected)
                return new OperationResult<bool>(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(setKey))
                return new OperationResult<bool>();

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(setKey), metricEntity, requestParams: new { SetKey = setKey, MemberOfSet = member }, longRequestTime: longTimeRequest))
            {
                return await ExecuteAsync(async () => await _redisDb.SetContainsAsync(setKey, member), new { setKey, member });
            }
        }

        public async Task<OperationResult<RedisValue>> GetFromHashAsync(string hashKey, string hashField, string metricEntity, TimeSpan? longTimeRequest = null)
        {
            if (!_connected)
                return new OperationResult<RedisValue>(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(hashKey) || string.IsNullOrEmpty(hashField))
                return new OperationResult<RedisValue>();

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(hashKey), metricEntity, requestParams: new { HashKey = hashKey, HashField = hashField }, longRequestTime: longTimeRequest))
            {
                return await ExecuteAsync(async () => await _redisDb.HashGetAsync(hashKey, hashField), new { hashKey, hashField });
            }
        }

        public async Task<OperationResult> InsertIntoHashAsync(string hashKey, string hashField, RedisValue value, string metricEntity, TimeSpan? longTimeRequest = null)
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(hashKey) || string.IsNullOrEmpty(hashField))
                return OperationResult.Ok;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(hashKey), metricEntity, requestParams: new { HashKey = hashKey, HashField = hashField }, longRequestTime: longTimeRequest))
            {
                return await ExecuteAsync(async () => await _redisDb.HashSetAsync(hashKey, hashField, value), new { hashKey, hashField, value });
            }
        }

        public async Task<OperationResult> DeleteFromHashAsync(string hashKey, string hashField, string metricEntity, TimeSpan? longTimeRequest = null)
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(hashKey) || string.IsNullOrEmpty(hashField))
                return OperationResult.Ok;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(hashKey), metricEntity, requestParams: new { HashKey = hashKey, HashField = hashField }, longRequestTime: longTimeRequest))
            {
                return await ExecuteAsync(async () => await _redisDb.HashDeleteAsync(hashKey, hashField), new { hashKey, hashField });
            }
        }

        public async Task<OperationResult> ExpireAsync(string key, DateTime expiration, string metricEntity, TimeSpan? longRequestTime = null)
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(key))
                return OperationResult.Ok;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(key), metricEntity, requestParams: new {Key = key, Expiration = expiration}, longRequestTime: longRequestTime))
            {
                return await ExecuteAsync(async () => await _redisDb.KeyExpireAsync(key, expiration.ToUniversalTime()), new {key, expiration});
            }
        }

        public async Task<OperationResult> ExpireAsync(string key, TimeSpan ttl, string metricEntity, TimeSpan? longRequestTime = null)
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(key))
                return OperationResult.Ok;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(key), metricEntity, requestParams: new {Key = key, TimeToLive = ttl}, longRequestTime: longRequestTime))
            {
                return await ExecuteAsync(async () => await _redisDb.KeyExpireAsync(key, ttl), new {key, ttl});
            }
        }

        public async Task<OperationResult> SortedSetRemoveAsync(string sortedSetKey, string member, string metricEntity, TimeSpan? longTimeRequest = null)
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            if (string.IsNullOrWhiteSpace(sortedSetKey))
                return new OperationResult(ActionStatus.BadRequest, "sorted set key was not specified.");

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(
                GetTracingInfo(sortedSetKey),
                metricEntity,
                requestParams: new
                {
                    Value = member,
                    SortedSetKey = sortedSetKey
                },
                longRequestTime: longTimeRequest))
            {
                return await ExecuteAsync(async () => await _redisDb.SortedSetRemoveAsync(
                        sortedSetKey,
                        member),
                    new
                    {
                        RedisValue = member,
                        SortedSetKey = sortedSetKey,
                        MetricEntity = metricEntity
                    });
            }
        }

        public async Task<OperationResult> SortedSetAddAsync(string sortedSetKey, string member, double valueScore, string metricEntity, TimeSpan? longTimeRequest = null)
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            if (string.IsNullOrWhiteSpace(sortedSetKey))
                return new OperationResult(ActionStatus.BadRequest, "sorted set key was not specified.");

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(
                GetTracingInfo(sortedSetKey),
                metricEntity,
                requestParams: new
                {
                    Value = member,
                    SortedSetKey = sortedSetKey,
                    ValueScore = valueScore
                },
                longRequestTime: longTimeRequest))
            {
                return await ExecuteAsync(async () => await _redisDb.SortedSetAddAsync(
                    sortedSetKey,
                    member,
                    valueScore), new
                {
                    RedisValue = member,
                    SortedSetKey = sortedSetKey,
                    ValueScore = valueScore,
                    MetricEntity = metricEntity
                });
            }
        }

        private async Task<OperationResult<bool>> InsertPrivateAsync<T>(T redisValue, string key, TimeSpan? timeToLive, string metricEntity, TimeSpan? longTimeRequest = null, When when = When.Always)
        {
            if (!_connected)
                return new OperationResult<bool>(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(key))
                return new OperationResult<bool>();

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(key), metricEntity,
                                                                               requestParams: new { Value = redisValue, Key = key, TimeToLive = timeToLive },
                                                                               longRequestTime: longTimeRequest))
            {
                return await ExecuteAsync(async () => await _redisDb.StringSetAsync(key, Serializer.Serialize(redisValue), timeToLive, when), new { redisValue, key });
            }
        }

        public async Task<OperationResult> InsertTypeAsHashAsync<T>(string hashKey, T data, string metricEntity,
            TimeSpan? longTimeRequest = null, TimeSpan? ttl = null) where T : class, new()
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(hashKey))
                return OperationResult.Ok;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(hashKey), metricEntity,
                requestParams: new {RedisValues = data, HashKey = hashKey}, longRequestTime: longTimeRequest))
            {
                var fields = ToHashEntries(data).ToArray();

                var transaction = _redisDb.CreateTransaction();
                transaction.HashSetAsync(hashKey, fields).Forget();
                transaction.KeyExpireAsync(hashKey, ttl ?? Options.TimeToLive).Forget();
                
                return await ExecuteAsync(async () => await transaction.ExecuteAsync(), new { hashKey, data });
            }
        }

        public async Task<OperationResult> InsertManyFieldsToHashAsync<TKey, TValue>(string hashKey,
            List<KeyValuePair<TKey, TValue>> fieldsToInsert, string metricEntity,
            TimeSpan? longTimeRequest = null, TimeSpan? ttl = null)
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(hashKey))
                return OperationResult.Ok;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(hashKey), metricEntity,
                requestParams: new {RedisValues = fieldsToInsert, HashKey = hashKey}, longRequestTime: longTimeRequest))
            {
                var fields = fieldsToInsert.Select(kvp => new HashEntry(kvp.Key.ToString(), Serializer.Serialize(kvp.Value))).ToArray();

                var transaction = _redisDb.CreateTransaction();
                transaction.HashSetAsync(hashKey, fields).Forget();
                transaction.KeyExpireAsync(hashKey, ttl ?? Options.TimeToLive).Forget();
                
                return await ExecuteAsync(async () => await transaction.ExecuteAsync(), new { hashKey, fieldsToInsert });
            }
        }

        public async Task<OperationResult> InsertManyFieldsToManyHashesAsync<TKey, TValue>(
            Dictionary<string, List<KeyValuePair<TKey, TValue>>> valuesByHashKeys,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (valuesByHashKeys.Count == 0)
                return OperationResult.Ok;

            using var metric = _metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(valuesByHashKeys.FirstOrDefault().Key),
                metricEntity,
                requestParams: new {RedisValues = valuesByHashKeys}, longRequestTime: longTimeRequest);

            var transaction = _redisDb.CreateTransaction();
            foreach (var (hashKey, hashValues) in valuesByHashKeys)
            {
                transaction.HashSetAsync(hashKey,
                    hashValues.Select(kvp => new HashEntry(kvp.Key.ToString(), Serializer.Serialize(kvp.Value))).ToArray()).Forget();
                if (Options.TimeToLive != null)
                    transaction.KeyExpireAsync(hashKey, Options.TimeToLive).Forget();
            }

            return await ExecuteAsync(async () => await transaction.ExecuteAsync(), new { RedisValues = valuesByHashKeys, MetricEntity = metricEntity });
        }

        //TODO: Проверить работу через хеши. При больших объемах работает быстрее, чем сеты, но существует вопрос при вставке одиночной записи
        public async Task<OperationResult> InsertHashAsync<T>(List<T> manyRedisValues, string hashKey, string metricEntity, TimeSpan? longTimeRequest = null) where T : ICacheEntity
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(hashKey))
                return OperationResult.Ok;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(hashKey), metricEntity, requestParams: new { RedisValues = manyRedisValues, HashKey = hashKey }, longRequestTime: longTimeRequest))
            {
                return await ExecuteAsync(async () => await _redisDb.HashSetAsync(hashKey,
                    manyRedisValues
                                .Select(value => new HashEntry(value.GetKey(), Serializer.Serialize(value)))
                                .ToArray()), new { manyRedisValues, hashKey });
            }
        }

        public async Task<OperationResult<T>> GetFromHashAsync<T>(string hashKey, string hashField, string metricEntity, TimeSpan? longTimeRequest = null)
        {
            if (!_connected)
                return new OperationResult<T>(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(hashKey) || string.IsNullOrEmpty(hashField))
                return new OperationResult<T>();

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(hashKey), metricEntity, requestParams: new { HashKey = hashKey, HashField = hashField }, longRequestTime: longTimeRequest))
            {
                var operationResult = await ExecuteAsync(async () => await _redisDb.HashGetAsync(hashKey, hashField), new { hashKey, hashField });

                if (!operationResult.Success)
                {
                    _counter.Miss();

                    return new OperationResult<T>(operationResult.ActionStatus);
                }

                _counter.Hit();
                var value = Serializer.Deserialize<T>(operationResult.Value);
                return new OperationResult<T>(value);
            }
        }

        public async Task<OperationResult<T>> GetTypeFromHashAsync<T>(string hashKey, string metricEntity, TimeSpan? longTimeRequest = null) where T: class, new()
        {
            if (!_connected)
                return new OperationResult<T>(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(hashKey))
                return new OperationResult<T>();

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(hashKey), metricEntity,
                requestParams: new {HashKey = hashKey}, longRequestTime: longTimeRequest))
            {
                var operationResult = await ExecuteAsync(async () => await _redisDb.HashGetAllAsync(hashKey), hashKey);
                if (!operationResult.Success)
                    return new OperationResult<T>(operationResult.ActionStatus);

                if (operationResult.Value.Length == 0)
                {
                    return new OperationResult<T>(ActionStatus.NotFound);
                }

                _counter.Hit();

                var result = FromRedisHash<T>(operationResult.Value);
                return new OperationResult<T>(result);
            }
        }

        public async Task<OperationResult<List<KeyValuePair<TKey, TValue>>>> GetManyFieldsFromHashAsync<TKey, TValue>(string hashKey, string metricEntity, TimeSpan? longTimeRequest = null)
        {
            if (!_connected)
                return new OperationResult<List<KeyValuePair<TKey, TValue>>>(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(hashKey))
                return new OperationResult<List<KeyValuePair<TKey, TValue>>>();

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(hashKey), metricEntity,
                requestParams: new {HashKey = hashKey}, longRequestTime: longTimeRequest))
            {
                var operationResult = await ExecuteAsync(async () => await _redisDb.HashGetAllAsync(hashKey), hashKey);
                if (!operationResult.Success)
                    return new OperationResult<List<KeyValuePair<TKey, TValue>>>(operationResult.ActionStatus);

                if (operationResult.Value.Length == 0)
                {
                    return new OperationResult<List<KeyValuePair<TKey, TValue>>>(ActionStatus.NotFound);
                }

                _counter.Hit();

                var result = operationResult.Value.Select(h =>
                {
                    if(!h.Name.ToString().TryConvert(out TKey key))
                        throw new ArgumentException($"Не удалось сконвертировать HashFieldName={h.Name} в тип {typeof(TKey)}");
                    return new KeyValuePair<TKey, TValue>(key, Serializer.Deserialize<TValue>(h.Value));
                }).ToList();
                return new OperationResult<List<KeyValuePair<TKey, TValue>>>(result);
            }
        }


        public async Task<OperationResult<List<T>>> GetManyFromHashAsync<T>(string hashKey, string metricEntity, TimeSpan? longTimeRequest = null)
        {
            if (!_connected)
                return new OperationResult<List<T>>(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(hashKey))
                return new OperationResult<List<T>>();

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(hashKey), metricEntity, requestParams: new { HashKey = hashKey }, longRequestTime: longTimeRequest))
            {
                var operationResult = await ExecuteAsync(async () => await _redisDb.HashValuesAsync(hashKey), hashKey);
                if (operationResult.Success)
                {
                    if (operationResult.Value.Length == 0)
                    {
                        return new OperationResult<List<T>>(ActionStatus.NotFound);
                    }
                    var result = operationResult.Value.Where(value => value.HasValue).Select(value => Serializer.Deserialize<T>(value)).ToList();

                    var amountOfFoundValues = operationResult.Value.Count(value => value.HasValue);
                    _counter.Hit(amountOfFoundValues);

                    return new OperationResult<List<T>>(result);
                }

                return new OperationResult<List<T>>(operationResult.ActionStatus);
            }
        }

        public async Task<OperationResult<double>> IncreaseAsync(string key, double value, string metricEntity, TimeSpan? longTimeRequest = null)
        {
            if (!_connected)
                return new OperationResult<double>(ActionStatus.InternalOptionalServerUnavailable);
            if (string.IsNullOrEmpty(key))
                return new OperationResult<double>();

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(key), metricEntity, requestParams: new { Key = key, Value = value }, longRequestTime: longTimeRequest))
            {
                var transaction = _redisDb.CreateTransaction();
                var incrementTransaction = transaction.StringIncrementAsync(key, value);

                var operation = await ExecuteAsync(async () => await transaction.ExecuteAsync(), new { key, value });
                if (!operation.Success)
                    return new OperationResult<double>(operation);

                return new OperationResult<double>(incrementTransaction.Result);
            }
        }


        #region ByScript

        public async Task<OperationResult> InsertManyByScriptAsync<T>([NotNull] List<T> redisValue, string metricEntity, TimeSpan? longRequestTime = null) where T : ICacheEntity
        {
            if (!_connected)
                return new OperationResult<T>(ActionStatus.InternalOptionalServerUnavailable);

            return await _redisScriptCache.InsertManyByScriptAsync(redisValue, metricEntity, longRequestTime);
        }

        public async Task<OperationResult> InsertManyByScriptAsync<T>(Dictionary<string, T> redisValues, string metricEntity, TimeSpan? longRequestTime = null)
        {
            if (!_connected)
                return new OperationResult<T>(ActionStatus.InternalOptionalServerUnavailable);

            return await _redisScriptCache.InsertManyByScriptAsync(redisValues, metricEntity, longRequestTime);
        }

        #endregion


        private async Task<OperationResult> ExecuteAsync(Func<Task> func, object context)
        {
            if (!_connected)
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);

            return await base.ExecuteAsync(func, context, _circuitBreakerPolicy, _policy);
        }

        private async Task<OperationResult<T>> ExecuteAsync<T>(Func<Task<T>> func, object context)
        {
            if (!_connected)
                return new OperationResult<T>(ActionStatus.InternalOptionalServerUnavailable);

            return await base.ExecuteAsync(func, context, _circuitBreakerPolicy, _policy);
        }


        private T FromRedisHash<T>(HashEntry[] hashEntries) where T : class, new()
        {
            if (hashEntries.Length == 0)
                return default;

            var propertyInfos = typeof(T).GetProperties().ToDictionary(p=> p.Name);

            var result = new T();
            foreach (var hashEntry in hashEntries)
            {
                if (!propertyInfos.TryGetValue(hashEntry.Name, out var propertyInfo))
                    continue;

                propertyInfo.SetValue(result, Serializer.Deserialize(hashEntry.Value, propertyInfo.PropertyType));
            }

            return result;
        }

        private IEnumerable<HashEntry> ToHashEntries<T>(T entity) where T : class, new()
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            return typeof(T).GetProperties().Select(p => new HashEntry(p.Name,
                Serializer.Serialize(p.GetValue(entity), p.PropertyType)));
        }
    }
}