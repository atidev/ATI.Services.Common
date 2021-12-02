using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Serializers;
using ATI.Services.Common.Tracing;
using NLog;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;
using StackExchange.Redis;

namespace ATI.Services.Common.Caching.Redis
{
    public abstract class BaseRedisCache
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        protected readonly ISerializer Serializer;

        protected RedisOptions Options;

        protected BaseRedisCache(ISerializer serializer)
        {
            Serializer = serializer;
        }

        protected Dictionary<string, string> GetTracingInfo(string key) => TraceHelper.GetRedisTracingInfo(Options.ConnectionString, key);
        
        protected async Task<OperationResult> ExecuteAsync(
            Func<Task> func, 
            object context, 
            CircuitBreakerPolicy circuitBreakerPolicy, 
            Policy policy)
        {
            try
            {
                if (circuitBreakerPolicy.CircuitState == CircuitState.Open)
                {
                    return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
                }

                await policy.ExecuteAsync(func);

                return OperationResult.Ok;
            }
            catch (TimeoutRejectedException ex)
            {
                Logger.ErrorWithObject(ex, new { DelegateName = func?.Method.Name, ReturnType = func?.Method.ReturnType.Name, Context = context });
                return new OperationResult(ActionStatus.Timeout);
            }
            catch (Exception ex)
            {
                Logger.ErrorWithObject(ex, new { DelegateName = func?.Method.Name, ReturnType = func?.Method.ReturnType.Name, Context = context });
                return new OperationResult(ActionStatus.InternalOptionalServerUnavailable);
            }
        }

        protected async Task<OperationResult<T>> ExecuteAsync<T>(Func<Task<T>> func, 
            object context,
            CircuitBreakerPolicy circuitBreakerPolicy, 
            Policy policy)
        {
            try
            {
                if (circuitBreakerPolicy.CircuitState == CircuitState.Open)
                {
                    return new OperationResult<T>(ActionStatus.InternalOptionalServerUnavailable);
                }

                var result = await policy.ExecuteAsync(func);

                return result == null || result is RedisValue resultAsRedisValue && resultAsRedisValue.IsNull
                    ? new OperationResult<T>(ActionStatus.NotFound)
                    : new OperationResult<T>(result);
            }
            catch (TimeoutRejectedException ex)
            {
                Logger.ErrorWithObject(ex, new { DelegateName = func?.Method.Name, ReturnType = func?.Method.ReturnType.Name, Context = context });
                return new OperationResult<T>(ActionStatus.Timeout);
            }
            catch (Exception ex)
            {
                Logger.ErrorWithObject(ex, new { DelegateName = func?.Method.Name, ReturnType = func?.Method.ReturnType.Name, Context = context });
                return new OperationResult<T>(ActionStatus.InternalOptionalServerUnavailable);
            }
        }
    }
}