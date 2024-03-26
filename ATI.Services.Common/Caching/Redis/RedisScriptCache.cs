using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Caching.Redis.Abstractions;
using ATI.Services.Common.Metrics;
using JetBrains.Annotations;
using Polly.CircuitBreaker;
using Polly.Wrap;
using StackExchange.Redis;

namespace ATI.Services.Common.Caching.Redis;

internal class RedisScriptCache : BaseRedisCache
{
    private readonly MetricsInstance _metrics;
    private readonly IDatabase _redisDb;
    private readonly AsyncCircuitBreakerPolicy _circuitBreakerPolicy;
    private readonly AsyncPolicyWrap _policy;

    /// <summary>
    /// Инициализируем готовыми Policy, чтобы они разделяли общий CircuitBreaker - иначе у них будут разные State с RedisCache
    /// </summary>
    /// <param name="redisDb"></param>
    /// <param name="redisOptions"></param>
    /// <param name="metrics"></param>
    /// <param name="circuitBreakerPolicy"></param>
    /// <param name="policy"></param>
    public RedisScriptCache(
        IDatabase redisDb,
        RedisOptions redisOptions,
        MetricsInstance metrics,
        AsyncCircuitBreakerPolicy circuitBreakerPolicy,
        AsyncPolicyWrap policy,
        IRedisSerializer serializer
    )
    {
        Options = redisOptions;
        _redisDb = redisDb;
        _metrics = metrics;
        _circuitBreakerPolicy = circuitBreakerPolicy;
        _policy = policy;
        Serializer = serializer;
    }

    public async Task<OperationResult> InsertManyByScriptAsync<T>([NotNull] List<T> redisValue, string metricEntity,
        TimeSpan? longRequestTime = null) where T : ICacheEntity
    {
        if (redisValue.Count < 0)
            return OperationResult.Ok;

        using (_metrics.CreateMetricsTimerWithLogging(metricEntity,
                   requestParams: new { RedisValues = redisValue }, longRequestTime: longRequestTime))
        {
            var result = await InsertManyByScriptAsync(redisValue.Select(v => v.GetKey()), redisValue);
            return result;
        }
    }

    public async Task<OperationResult> InsertManyByScriptAsync<T>(Dictionary<string, T> redisValues,
        string metricEntity, TimeSpan? longRequestTime = null)
    {
        if (redisValues == null || redisValues.Count == 0)
            return OperationResult.Ok;

        using (_metrics.CreateMetricsTimerWithLogging(metricEntity,
                   requestParams: new { RedisValues = redisValues },
                   longRequestTime: longRequestTime))
        {
            var result = await InsertManyByScriptAsync(redisValues.Keys, redisValues.Values);
            return result;
        }
    }

    private async Task<OperationResult> InsertManyByScriptAsync<T>(IEnumerable<string> keys, ICollection<T> values)
    {
        var redisKeys = keys.Select(k => (RedisKey)k).ToArray();

        var redisValues = new RedisValue[values.Count + 1];
        var i = 0;
        foreach (var value in values)
        {
            redisValues[i] = Serializer.Serialize(value);
            i++;
        }

        // Последний параметр - длина жизни ключа в миллисекундах
        redisValues[^1] = (int)(Options.TimeToLive?.TotalMilliseconds ?? -1);

        RedisMetadata.ScriptShaByScriptType.TryGetValue(RedisMetadata.InsertManyScriptKey, out var scriptSha);

        var result = await ExecuteEvalShaAsync(scriptSha, RedisLuaScripts.InsertMany, redisKeys, redisValues);
        if (result.Success)
            RedisMetadata.ScriptShaByScriptType.AddOrUpdate(RedisMetadata.InsertManyScriptKey, result.Value,
                (_, _) => result.Value);

        return result;
    }

    /// <summary>
    /// Execute script by SHA <paramref name="scriptSha"/>.
    /// </summary>
    /// <param name="scriptSha">Script SHA.</param>
    /// <param name="script">Текст скрипта для загрузки.</param>
    /// <param name="keys">Список ключей.</param>
    /// <param name="arguments">Список параметров.</param>
    /// <returns>Возвращает хэш переданного в <paramref name="script"/> скрипта.</returns>
    private async Task<OperationResult<byte[]>> ExecuteEvalShaAsync(byte[] scriptSha, string script, RedisKey[] keys,
        params RedisValue[] arguments)
    {
        if (scriptSha == null)
            return await ExecuteEvalScriptAsync(script, keys, arguments);

        var result = await ExecuteAsync(async () => await _redisDb.ScriptEvaluateAsync(scriptSha, keys, arguments),
            keys, _circuitBreakerPolicy, _policy);
        return result.Success
            ? new OperationResult<byte[]>(scriptSha)
            : await ExecuteEvalScriptAsync(script, keys, arguments);
    }

    private async Task<OperationResult<byte[]>> ExecuteEvalScriptAsync(string script, RedisKey[] keys,
        params RedisValue[] arguments)
    {
        var result = await ExecuteAsync(async () => await _redisDb.ScriptEvaluateAsync(script, keys, arguments), keys,
            _circuitBreakerPolicy, _policy);
        return result.Success
            ? new OperationResult<byte[]>(SHA1.HashData(Encoding.UTF8.GetBytes(script)))
            : new OperationResult<byte[]>(result);
    }
}