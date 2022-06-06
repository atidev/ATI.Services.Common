using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Metrics;
using ATI.Services.Common.Tracing;
using Dapper;
using JetBrains.Annotations;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using NLog;

namespace ATI.Services.Common.Sql
{
    [PublicAPI]
    public class DapperDb
    {
        private const string ReturnValueFieldName = "RETURN_VALUE";
        private readonly DataBaseOptions _options;
        private readonly MetricsTracingFactory _metricsTracingFactory;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private const string ReadMetricTypeLabel = "read";
        private const string ConvertDataMetricTypeLabel = "convert";
        private const string QueryMetricTypeLabel = "query";
        private const string FullMetricTypeLabel = "full";

        public DapperDb(DataBaseOptions options)
        {
            _options = options;
            _metricsTracingFactory = MetricsTracingFactory.CreateSqlMetricsFactory(nameof(DapperDb), _options.LongTimeRequest, "type");
        }

        private Dictionary<string, string> GetTracingInfo(string procName) =>
            TraceHelper.GetSQLTracingInfo(procName, _options.ConnectionString);
        
        public async Task<OperationResult<List<T>>> ExecuteListAsync<T>(
            string procedureName,
            DynamicParameters parameters,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            SqlMapper.GridReader reader = null;
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                    metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                    longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            reader = await connection.QueryMultipleAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        List<T> result;
                        
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ReadMetricTypeLabel))
                        {
                            result = (await reader.ReadAsync<T>()).AsList();
                        }
                        
                        reader.Dispose();
                        
                        return new OperationResult<List<T>>(result);
                    }
                }
            }
            catch (Exception e)
            {
                reader?.Dispose();
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<List<T>>(ActionStatus.InternalServerError, e.Message);
            }
        }

        public async Task<OperationResult<List<T>>> ExecuteListWithReturnValueAsync<T>(
            string procedureName,
            DynamicParameters parameters,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            SqlMapper.GridReader reader = null;
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                    metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                    longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);
                    parameters.Add(ReturnValueFieldName, 0, DbType.Int32, ParameterDirection.ReturnValue);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            reader = await connection.QueryMultipleAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        var returnValue = parameters.Get<int>(ReturnValueFieldName);

                        if ((ActionStatus) returnValue != ActionStatus.Ok) 
                            return new OperationResult<List<T>>(new List<T>(0), (ActionStatus) returnValue);
                        
                        List<T> result;
                        
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ReadMetricTypeLabel))
                        {
                            result = (await reader.ReadAsync<T>()).AsList();
                        }
                        
                        reader.Dispose();
                            
                        return new OperationResult<List<T>>(result, (ActionStatus)returnValue);

                    }
                }
            }
            catch (Exception e)
            {
                reader?.Dispose();
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<List<T>>(ActionStatus.InternalServerError, e.Message);
            }
        }

        public async Task<OperationResult<Dictionary<TKey, TValue>>> ExecuteScalarDictionaryAsync<TKey, TValue>(
            string procedureName,
            Func<dynamic, TKey> keySelector,
            Func<dynamic, TValue> valueSelector,
            DynamicParameters parameters,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            SqlMapper.GridReader reader = null;
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                    metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                    longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            reader = await connection.QueryMultipleAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        List<dynamic> result;
                        
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ReadMetricTypeLabel))
                        {
                            result = (await reader.ReadAsync()).AsList();
                        }
                        
                        reader.Dispose();

                        return new OperationResult<Dictionary<TKey, TValue>>(result.ToDictionary(keySelector, valueSelector));
                    }
                }
            }
            catch (Exception e)
            {
                reader?.Dispose();
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<Dictionary<TKey, TValue>>(ActionStatus.InternalServerError, e.Message);
            }
        }

        public async Task<OperationResult> ExecuteWithReturnValueAsync(
            string procedureName,
            DynamicParameters parameters,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                    metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                    longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);
                    parameters.Add(ReturnValueFieldName, 0, DbType.Int32, ParameterDirection.ReturnValue);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            await connection.ExecuteAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        return new OperationResult((ActionStatus)parameters.Get<int>(ReturnValueFieldName));
                    }
                }
            }
            catch (Exception e)
            {
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult(ActionStatus.InternalServerError, e.Message);
            }
        }
        
        public async Task<OperationResult<TResult>> ExecuteWithReturnValueAsync<TResult>(
            string procedureName,
            DynamicParameters parameters,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                           metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                           longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);
                    parameters.Add(ReturnValueFieldName, 0, DbType.Int32, ParameterDirection.ReturnValue);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                                   metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                                   longTimeRequest, QueryMetricTypeLabel))
                        {
                            await connection.ExecuteAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        return new OperationResult<TResult>(parameters.Get<TResult>(ReturnValueFieldName));
                    }
                }
            }
            catch (Exception e)
            {
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<TResult>(ActionStatus.InternalServerError, e.Message);
            }
        }

        public async Task<OperationResult<T>> ExecuteObjectAsync<T>(
            string procedureName,
            DynamicParameters parameters,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            SqlMapper.GridReader reader = null;
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                    metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                    longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            reader = await connection.QueryMultipleAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        T result;
                        
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ReadMetricTypeLabel))
                        {
                            result = await reader.ReadFirstOrDefaultAsync<T>();
                        }
                        
                        reader.Dispose();

                        return new OperationResult<T>(result);
                    }
                }
            }
            catch (Exception e)
            {
                reader?.Dispose();
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<T>(ActionStatus.InternalServerError, e.Message);
            }
        }

        public async Task<OperationResult<T>> ExecuteObjectWithReturnValueAsync<T>(
            string procedureName,
            DynamicParameters parameters,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            SqlMapper.GridReader reader = null;
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                    metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                    longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);
                    parameters.Add(ReturnValueFieldName, 0, DbType.Int32, ParameterDirection.ReturnValue);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, 
                            QueryMetricTypeLabel))
                        {
                            reader = await connection.QueryMultipleAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        var returnValue = parameters.Get<int>(ReturnValueFieldName);
                        
                        T result;
                        
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ReadMetricTypeLabel))
                        {
                            result = await reader.ReadFirstOrDefaultAsync<T>();
                        }
                        
                        reader.Dispose();

                        return new OperationResult<T>(result, (ActionStatus)returnValue);
                    }
                }
            }
            catch (Exception e)
            {
                reader?.Dispose();
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<T>(ActionStatus.InternalServerError, e.Message);
            }
        }

        public async Task<OperationResult<List<T>>> ExecuteResultSetWithReturnValueAsync<T>(
            string procedureName,
            DynamicParameters parameters,
            Func<SqlMapper.GridReader, Task<List<T>>> convertData,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            SqlMapper.GridReader reader = null;
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo, metricEntity, procedureName,
                    new { StoredProcedure = procedureName, Parameters = parameters }, longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);
                    parameters.Add(ReturnValueFieldName, 0, DbType.Int32, ParameterDirection.ReturnValue);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            reader = await connection.QueryMultipleAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        var returnValue = parameters.Get<int>(ReturnValueFieldName);

                        if ((ActionStatus) returnValue != ActionStatus.Ok)
                            return new OperationResult<List<T>>(new List<T>(), (ActionStatus) returnValue);

                        List<T> result;
                        
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ReadMetricTypeLabel))
                        {
                            result = await convertData(reader);
                        }
                        
                        reader.Dispose();

                        return new OperationResult<List<T>>(result);
                    }
                }
            }
            catch (Exception e)
            {
                reader?.Dispose();
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<List<T>>(ActionStatus.InternalServerError, e.Message);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="parameters"></param>
        /// <param name="convertData">TimersWrapper нужен для того, чтобы замерять время выполнения кастомной логики по маппингу внутри ConvertData после всех ReadAsync</param>
        /// <param name="metricEntity"></param>
        /// <param name="longTimeRequest"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<OperationResult<List<T>>> ExecuteResultSetWithReturnValueAsync<T>(
            string procedureName,
            DynamicParameters parameters,
            Func<SqlMapper.GridReader, TimersWrapper, Task<List<T>>> convertData,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            SqlMapper.GridReader reader = null;
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo, metricEntity, procedureName,
                    new { StoredProcedure = procedureName, Parameters = parameters }, longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);
                    parameters.Add(ReturnValueFieldName, 0, DbType.Int32, ParameterDirection.ReturnValue);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            reader = await connection.QueryMultipleAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        var returnValue = parameters.Get<int>(ReturnValueFieldName);

                        if ((ActionStatus) returnValue != ActionStatus.Ok)
                            return new OperationResult<List<T>>(new List<T>(), (ActionStatus) returnValue);

                        List<T> result;
                        
                        using (_metricsTracingFactory.CreateTracingWithDelayedLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ReadMetricTypeLabel))
                        using (var convertDataTimer = _metricsTracingFactory.CreateTracingWithDelayedLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ConvertDataMetricTypeLabel))
                        {
                            result = await convertData(reader, convertDataTimer);
                        }
                        
                        reader.Dispose();

                        return new OperationResult<List<T>>(result);
                    }
                }
            }
            catch (Exception e)
            {
                reader?.Dispose();
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<List<T>>(ActionStatus.InternalServerError, e.Message);
            }
        }
        
        public async Task<OperationResult<List<T>>> ExecuteResultSetAsync<T>(
            string procedureName,
            DynamicParameters parameters,
            Func<SqlMapper.GridReader, Task<List<T>>> convertData,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            SqlMapper.GridReader reader = null;
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo, metricEntity, procedureName,
                    new { StoredProcedure = procedureName, Parameters = parameters }, longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            reader = await connection.QueryMultipleAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        List<T> result;
                        
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ReadMetricTypeLabel))
                        {
                            result = await convertData(reader);
                        }
                        
                        reader.Dispose();

                        return new OperationResult<List<T>>(result);
                    }
                }
            }
            catch (Exception e)
            {
                reader?.Dispose();
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<List<T>>(ActionStatus.InternalServerError, e.Message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="parameters"></param>
        /// <param name="convertData">TimersWrapper нужен для того, чтобы замерять время выполнения кастомной логики по маппингу внутри ConvertData после всех ReadAsync</param>
        /// <param name="metricEntity"></param>
        /// <param name="longTimeRequest"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<OperationResult<List<T>>> ExecuteResultSetAsync<T>(
            string procedureName,
            DynamicParameters parameters,
            Func<SqlMapper.GridReader, TimersWrapper, Task<List<T>>> convertData,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            SqlMapper.GridReader reader = null;
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo, metricEntity, procedureName,
                    new { StoredProcedure = procedureName, Parameters = parameters }, longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            reader = await connection.QueryMultipleAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        List<T> result;
                        
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ReadMetricTypeLabel))
                        using (var convertDataTimer = _metricsTracingFactory.CreateTracingWithDelayedLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ConvertDataMetricTypeLabel))
                        {
                            result = await convertData(reader, convertDataTimer);
                        }
                        
                        reader.Dispose();

                        return new OperationResult<List<T>>(result);
                    }
                }
            }
            catch (Exception e)
            {
                reader?.Dispose();
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<List<T>>(ActionStatus.InternalServerError, e.Message);
            }
        }

        public async Task<OperationResult<T>> ExecuteScalarAsync<T>(
            string procedureName,
            DynamicParameters parameters,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo, metricEntity, procedureName,
                    new { StoredProcedure = procedureName, Parameters = parameters }, longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            return new OperationResult<T>(await connection.ExecuteScalarAsync<T>(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<T>(ActionStatus.InternalServerError, e.Message);
            }
        }

        public async Task<OperationResult<T>> ExecuteScalarWithReturnValueAsync<T>(
            string procedureName,
            DynamicParameters parameters,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo, metricEntity, procedureName,
                    new { StoredProcedure = procedureName, Parameters = parameters }, longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);
                    parameters.Add(ReturnValueFieldName, 0, DbType.Int32, ParameterDirection.ReturnValue);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        T result;
                        
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            result = await connection.ExecuteScalarAsync<T>(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        var returnValue = parameters.Get<int>(ReturnValueFieldName);

                        return (ActionStatus)returnValue == ActionStatus.Ok
                            ? new OperationResult<T>(result)
                            : new OperationResult<T>(default, (ActionStatus)returnValue);
                    }
                }
            }
            catch (Exception e)
            {
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<T>(ActionStatus.InternalServerError, e.Message);
            }
        }

        public async Task<OperationResult> ExecuteNonQueryAsync(
            string procedureName,
            DynamicParameters parameters,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo, metricEntity, procedureName,
                    new { StoredProcedure = procedureName, Parameters = parameters }, longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            await connection.ExecuteAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        return new OperationResult(ActionStatus.Ok);
                    }
                }
            }
            catch (Exception e)
            {
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult(ActionStatus.InternalServerError, e.Message);
            }
        }

        public async Task<OperationResult> ExecuteNonQueryWithReturnValueAsync(
            string procedureName,
            DynamicParameters parameters,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo, metricEntity, procedureName,
                    new { StoredProcedure = procedureName, Parameters = parameters }, longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);
                    parameters.Add(ReturnValueFieldName, 0, DbType.Int32, ParameterDirection.ReturnValue);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            await connection.ExecuteAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        var returnValue = parameters.Get<int>(ReturnValueFieldName);

                        return new OperationResult((ActionStatus)returnValue);
                    }
                }
            }
            catch (Exception e)
            {
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult(ActionStatus.InternalServerError, e.Message);
            }
        }

        public async Task<OperationResult<T>> ExecuteObjectAsync<T>(
            string procedureName,
            DynamicParameters parameters,
            Func<SqlMapper.GridReader, Task<T>> convertData,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            SqlMapper.GridReader reader = null;
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo, metricEntity, procedureName,
                    new { StoredProcedure = procedureName, Parameters = parameters }, longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);

                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            reader = await connection.QueryMultipleAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        T result;
                        
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ReadMetricTypeLabel))
                        {
                            result = await convertData(reader);
                        }
                        
                        reader.Dispose();

                        return new OperationResult<T>(result);
                    }
                }
            }
            catch (Exception e)
            {
                reader?.Dispose();
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<T>(ActionStatus.InternalServerError, e.Message);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="parameters"></param>
        /// <param name="convertData">TimersWrapper нужен для того, чтобы замерять время выполнения кастомной логики по маппингу внутри ConvertData после всех ReadAsync</param>
        /// <param name="metricEntity"></param>
        /// <param name="longTimeRequest"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<OperationResult<T>> ExecuteObjectAsync<T>(
            string procedureName,
            DynamicParameters parameters,
            Func<SqlMapper.GridReader, TimersWrapper, Task<T>> convertData,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            SqlMapper.GridReader reader = null;
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo, metricEntity, procedureName,
                    new { StoredProcedure = procedureName, Parameters = parameters }, longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);

                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            reader = await connection.QueryMultipleAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        T result;
                        
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ReadMetricTypeLabel))
                        using (var convertDataTimer = _metricsTracingFactory.CreateTracingWithDelayedLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ConvertDataMetricTypeLabel))
                        {
                            result = await convertData(reader, convertDataTimer);
                        }
                        
                        reader.Dispose();

                        return new OperationResult<T>(result);
                    }
                }
            }
            catch (Exception e)
            {
                reader?.Dispose();
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<T>(ActionStatus.InternalServerError, e.Message);
            }
        }

        public async Task<OperationResult<T>> ExecuteObjectWithReturnValueAsync<T>(
            string procedureName,
            DynamicParameters parameters,
            Func<SqlMapper.GridReader, Task<T>> convertData,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            SqlMapper.GridReader reader = null;
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo, metricEntity, procedureName,
                    new { StoredProcedure = procedureName, Parameters = parameters }, longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);

                    parameters.Add(ReturnValueFieldName, 0, DbType.Int32, ParameterDirection.ReturnValue);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            reader = await connection.QueryMultipleAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        var returnValue = parameters.Get<int>(ReturnValueFieldName);

                        if ((ActionStatus) returnValue != ActionStatus.Ok)
                            return new OperationResult<T>(default, (ActionStatus) returnValue);

                        T result;
                        
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ReadMetricTypeLabel))
                        {
                            result = await convertData(reader);
                        }
                        
                        reader.Dispose();
                        
                        return new OperationResult<T>(result);
                    }
                }
            }
            catch (Exception e)
            {
                reader?.Dispose();
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<T>(ActionStatus.InternalServerError, e.Message);
            }
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="procedureName"></param>
        /// <param name="parameters"></param>
        /// <param name="convertData">TimersWrapper нужен для того, чтобы замерять время выполнения кастомной логики по маппингу внутри ConvertData после всех ReadAsync</param>
        /// <param name="metricEntity"></param>
        /// <param name="longTimeRequest"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<OperationResult<T>> ExecuteObjectWithReturnValueAsync<T>(
            string procedureName,
            DynamicParameters parameters,
            Func<SqlMapper.GridReader, TimersWrapper, Task<T>> convertData,
            string metricEntity,
            TimeSpan? longTimeRequest = null)
        {
            SqlMapper.GridReader reader = null;
            try
            {
                var tracingInfo = GetTracingInfo(procedureName);
                
                using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo, metricEntity, procedureName,
                    new { StoredProcedure = procedureName, Parameters = parameters }, longTimeRequest, FullMetricTypeLabel))
                {
                    var timeout = GetTimeOut(procedureName);

                    parameters.Add(ReturnValueFieldName, 0, DbType.Int32, ParameterDirection.ReturnValue);
                    await using (var connection = new SqlConnection(_options.ConnectionString))
                    {
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, QueryMetricTypeLabel))
                        {
                            reader = await connection.QueryMultipleAsync(
                                procedureName,
                                parameters,
                                commandType: CommandType.StoredProcedure,
                                commandTimeout: timeout);
                        }

                        var returnValue = parameters.Get<int>(ReturnValueFieldName);

                        if ((ActionStatus) returnValue != ActionStatus.Ok)
                            return new OperationResult<T>(default, (ActionStatus) returnValue);

                        T result;
                        
                        using (_metricsTracingFactory.CreateTracingWithLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ReadMetricTypeLabel))
                        using (var convertDataTimer = _metricsTracingFactory.CreateTracingWithDelayedLoggingMetricsTimer(tracingInfo,
                            metricEntity, procedureName, new { StoredProcedure = procedureName, Parameters = parameters },
                            longTimeRequest, ConvertDataMetricTypeLabel))
                        {
                            result = await convertData(reader, convertDataTimer);
                        }
                        
                        reader.Dispose();
                        
                        return new OperationResult<T>(result);
                    }
                }
            }
            catch (Exception e)
            {
                reader?.Dispose();
                LogWithParameters(e, procedureName, metricEntity, parameters);
                return new OperationResult<T>(ActionStatus.InternalServerError, e.Message);
            }
        }

        
        private int GetTimeOut(string procedureName)
        {
            return _options.TimeoutDictionary.TryGetValue(procedureName, out var tempTimeout)
                   ? tempTimeout
                   : _options.Timeout.Seconds;
        }
        
        private void LogWithParameters(Exception e, string procedureName, string metricEntity, DynamicParameters parameters)
        {
            var parametersWithValues = GetProcedureParametersWithValues(parameters);
            //С большой вероятностью лог может быть discarded на стороне logstash, если будет слишком много записей
            if (parametersWithValues != null && parametersWithValues.Count <= 20)
            {
                _logger.ErrorWithObject(e, 
                    new { procedureName, parameters = GetProcedureParametersWithValues(parameters), metricEntity });
            }
            else
            {
                _logger.ErrorWithObject(e, new { procedureName, metricEntity });
            }
        }

        private Dictionary<string, string> GetProcedureParametersWithValues(DynamicParameters parameters)
        {
            if (parameters == null || parameters.ParameterNames == null || !parameters.ParameterNames.Any())
                return null;

            var paramsArray = new Dictionary<string, string>(parameters.ParameterNames.Count());

            foreach (var name in parameters.ParameterNames)
            {
                var pValue = parameters.Get<dynamic>(name);
                // Для итераторов-оберток над табличным типом udt_ невозможно просто так получить значения для логирования, поэтому вытаскиваем их через рефлексию
                // В pValue хранится приватное поле data, в котором лежит наш TableWrapper
                // В TableWrapper лежат приватные поля - _sqlDataRecord , IEnumerable<T> _{name}, возможны и другие приватные структуры, если мы объявим их в нашем TableWrapper
                // Берем все, кроме _sqlDataRecord
                if (pValue is SqlMapper.ICustomQueryParameter tableWrapperParameter)
                {
                    try
                    {
                        var dataProperty = tableWrapperParameter.GetType()
                            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance).First(p => p.Name == "data");

                        var dataPropertyValue = dataProperty.GetValue(tableWrapperParameter);

                        var tableWrapperProperties = dataPropertyValue.GetType()
                            .GetFields(BindingFlags.NonPublic | BindingFlags.Instance).Where(p => p.Name != "_sqlDataRecord").ToList();

                        var resultValue = string.Join(',',
                            tableWrapperProperties.Select(pr =>
                                JsonConvert.SerializeObject(pr.GetValue(dataPropertyValue))
                            )
                        );

                        paramsArray.Add(name, resultValue);
                    }
                    catch (Exception ex)
                    {
                        paramsArray.Add(name, ex.Message);
                    }
                }
                else
                {
                    paramsArray.Add(name, pValue?.ToString());
                }
            }

            return paramsArray;
        }
    }
}
