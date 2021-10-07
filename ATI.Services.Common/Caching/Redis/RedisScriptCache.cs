using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Metrics;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Polly;
using Polly.CircuitBreaker;
using StackExchange.Redis;

namespace ATI.Services.Common.Caching.Redis
{
    internal class RedisScriptCache : BaseRedisCache
    {
        private readonly MetricsTracingFactory _metricsTracingFactory;
        private readonly IDatabase _redisDb;
        private readonly CircuitBreakerPolicy _circuitBreakerPolicy;
        private readonly Policy _policy;
        
        /// <summary>
        /// Инициализируем готовыми Policy, чтобы они разделяли общий CircuitBreaker - иначе у них будут разные State с RedisCache
        /// </summary>
        /// <param name="redisDb"></param>
        /// <param name="redisOptions"></param>
        /// <param name="metricsTracingFactory"></param>
        /// <param name="circuitBreakerPolicy"></param>
        /// <param name="policy"></param>
        public RedisScriptCache(
            IDatabase redisDb, 
            RedisOptions redisOptions, 
            MetricsTracingFactory metricsTracingFactory,
            CircuitBreakerPolicy circuitBreakerPolicy,
            Policy policy
            )
        {
            Options = redisOptions;
            _redisDb = redisDb;
            _metricsTracingFactory = metricsTracingFactory;
            _circuitBreakerPolicy = circuitBreakerPolicy;
            _policy = policy;
        }
        
        public async Task<OperationResult> InsertManyByScriptAsync<T>([NotNull] List<T> redisValue, string metricEntity, TimeSpan? longRequestTime = null) where T : ICacheEntity
        {
            if (redisValue.Count < 0)
                return OperationResult.Ok;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(redisValue.FirstOrDefault()?.GetKey()), metricEntity, 
                requestParams: new { RedisValues = redisValue }, longRequestTime: longRequestTime, additionalLabels: FullMetricTypeLabel))
            {
                var result = await InsertManyByScriptAsync(redisValue.Select(v => v.GetKey()), redisValue);
                return result;
            }
        }
        
        public async Task<OperationResult> InsertManyByScriptAsync<T>(Dictionary<string, T> redisValues, string metricEntity, TimeSpan? longRequestTime = null)
        {
            if (redisValues == null || redisValues.Count == 0 )
                return OperationResult.Ok;

            using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(GetTracingInfo(redisValues.FirstOrDefault().Key), metricEntity,
                requestParams: new { RedisValues = redisValues },
                longRequestTime: longRequestTime, additionalLabels: FullMetricTypeLabel))
            {
                var result = await InsertManyByScriptAsync(redisValues.Keys, redisValues.Values);
                return result;
            }
        }
        
        private async Task<OperationResult> InsertManyByScriptAsync<T>(IEnumerable<string> keys, ICollection<T> values)
        {
            var redisKeys = keys.Select(k => (RedisKey) k).ToArray();

            var redisValues = new RedisValue[values.Count + 1];
            var i = 0;
            foreach (var value in values)
            {
                redisValues[i] = JsonConvert.SerializeObject(value);
                i++;
            }

            // Последний параметр - длина жизни ключа в миллисекундах
            redisValues[^1] = (int) (Options.TimeToLive?.TotalMilliseconds ?? -1);

            RedisMetadata.ScriptShaByScriptType.TryGetValue(RedisMetadata.InsertManyScriptKey, out var scriptSha);

            var result = await ExecuteEvalShaAsync(scriptSha, RedisLuaScripts.InsertMany, redisKeys, redisValues);
            if (result.Success)
                RedisMetadata.ScriptShaByScriptType.AddOrUpdate(RedisMetadata.InsertManyScriptKey, result.Value, (key, oldValue) => result.Value);

            return result;
            
        }

        /// <summary>
        /// Выполняет скрипт по хэшу <paramref name="scriptSha"/>.
        /// </summary>
        /// <param name="scriptSha">Хэш скрипта.</param>
        /// <param name="script">Текст скрипта для загрузки.</param>
        /// <param name="keys">Список ключей.</param>
        /// <param name="arguments">Список параметров.</param>
        /// <returns>Возвращает хэш переданного в <paramref name="script"/> скрипта.</returns>
        private async Task<OperationResult<byte[]>> ExecuteEvalShaAsync(byte[] scriptSha, string script, RedisKey[] keys, params RedisValue[] arguments)
        {
            if (scriptSha == null)
                return await ExecuteEvalScriptAsync(script, keys, arguments);

            var result = await ExecuteAsync(async () => await _redisDb.ScriptEvaluateAsync(scriptSha, keys, arguments), keys, _circuitBreakerPolicy, _policy);
            return result.Success
                ? new OperationResult<byte[]>(scriptSha)
                : await ExecuteEvalScriptAsync(script, keys, arguments);
        }

        private async Task<OperationResult<byte[]>> ExecuteEvalScriptAsync(string script, RedisKey[] keys,
            params RedisValue[] arguments)
        {
            var result = await ExecuteAsync(async () => await _redisDb.ScriptEvaluateAsync(script, keys, arguments), keys, _circuitBreakerPolicy, _policy);
            return result.Success
                ? new OperationResult<byte[]>(SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(script))) 
                : new OperationResult<byte[]>(result);
        }
    }
}