using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Metrics;
using Dapper;
using JetBrains.Annotations;
using NLog;
using Npgsql;

namespace ATI.Services.Common.Sql;

[PublicAPI]
public class PostgresDapper
{
    public DataBaseOptions Options { get; set; }

    private readonly MetricsInstance _metrics;
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private const string ReadMetricTypeLabel = "read";
    private const string ConvertDataMetricTypeLabel = "convert";
    private const string QueryMetricTypeLabel = "query";
    private const string FullMetricTypeLabel = "full";

    private const string FunctionQuery = "SELECT * FROM";
    private const string ProcedureQuery = "CALL";

    public  PostgresDapper(DataBaseOptions options, MetricsFactory metricsFactory)
    {
        Options = options;
        _metrics = metricsFactory.CreateSqlMetricsFactory(nameof(PostgresDapper), Options.LongTimeRequest, "type");
    }

    public async Task<OperationResult> ExecuteFunctionAsync(
        string functionName,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            functionName,
            GetFunctionQuery(parameters, functionName),
            parameters,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds, 
            cancellationToken);
    }

    public async Task<OperationResult> ExecuteProcedureAsync(
        string procedureName,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(
            procedureName,
            GetProcedureQuery(parameters, procedureName),
            parameters,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds, 
            cancellationToken);
    }

    private async Task<OperationResult> ExecuteAsync(
        string actionName,
        string query,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var _ = _metrics.CreateMetricsTimerWithLogging(
                metricEntity,
                actionName,
                new { Action = actionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            await using var connection = new NpgsqlConnection(ConnectionStringBuilder.BuildPostgresConnectionString(Options));

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            var timeout = timeoutInSeconds ?? GetTimeOut(actionName);

            using var disposable = _metrics.CreateMetricsTimerWithLogging(
                metricEntity,
                actionName,
                new { Action = actionName, Parameters = parameters },
                longTimeRequest, QueryMetricTypeLabel);

            await connection.ExecuteAsync(
                new CommandDefinition(
                    query, 
                    parameters, 
                    commandTimeout: timeout, 
                    cancellationToken: cancellationToken));
            return new OperationResult(ActionStatus.Ok);
        }
        catch (Exception e)
        {
            LogWithParameters(e, actionName, metricEntity, parameters);
            return new OperationResult(e);
        }
    }

    public async Task<OperationResult<T>> ExecuteFunctionObjectAsync<T>(
        string functionName,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var _ = _metrics.CreateMetricsTimerWithLogging(
                metricEntity,
                functionName,
                new { Action = functionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            var timeout = timeoutInSeconds ?? GetTimeOut(functionName);
            await using var connection = new NpgsqlConnection(ConnectionStringBuilder.BuildPostgresConnectionString(Options));

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            var result = await ExecuteTaskWithMetrics(connection.ExecuteScalarAsync<T>(
                new CommandDefinition(
                    GetFunctionQuery(parameters, functionName), 
                    parameters, 
                    commandTimeout: timeout, 
                    cancellationToken: cancellationToken)),
                _metrics.CreateMetricsTimerWithLogging(
                    metricEntity,
                    functionName,
                    new { Action = functionName, Parameters = parameters },
                    longTimeRequest,
                    QueryMetricTypeLabel));

            return new OperationResult<T>(result);
        }
        catch (Exception e)
        {
            LogWithParameters(e, functionName, metricEntity, parameters);
            return new OperationResult<T>(e);
        }
    }

    public async Task<OperationResult<T>> ExecuteFunctionObjectAsync<T>(
        string functionName,
        DynamicParameters parameters,
        Func<SqlMapper.GridReader, Task<T>> convertData,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var _ = _metrics.CreateMetricsTimerWithLogging(
                metricEntity,
                functionName,
                new { Action = functionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            var timeout = timeoutInSeconds ?? GetTimeOut(functionName);

            await using var connection = new NpgsqlConnection(ConnectionStringBuilder.BuildPostgresConnectionString(Options));

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            using var reader = await ExecuteTaskWithMetrics(
                connection.QueryMultipleAsync(
                    new CommandDefinition(
                        GetFunctionQuery(parameters, functionName), 
                        parameters, 
                        commandTimeout: timeout, 
                        cancellationToken: cancellationToken)),
                _metrics.CreateMetricsTimerWithLogging(
                    metricEntity,
                    functionName,
                    new { Action = functionName, Parameters = parameters },
                    longTimeRequest,
                    QueryMetricTypeLabel));

            using var disposable = _metrics.CreateMetricsTimerWithLogging(
                metricEntity,
                functionName,
                new { Action = functionName, Parameters = parameters },
                longTimeRequest,
                ReadMetricTypeLabel);

            var result = await convertData(reader);
            return new OperationResult<T>(result);
        }
        catch (Exception e)
        {
            LogWithParameters(e, functionName, metricEntity, parameters);
            return new OperationResult<T>(e);
        }
    }

    public async Task<OperationResult<T>> ExecuteFunctionObjectAsync<T>(
        string functionName,
        DynamicParameters parameters,
        Func<SqlMapper.GridReader, MetricsTimer, Task<T>> convertData,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var _ = _metrics.CreateMetricsTimerWithLogging(
                metricEntity,
                functionName,
                new { Action = functionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            var timeout = timeoutInSeconds ?? GetTimeOut(functionName);
            await using var connection = new NpgsqlConnection(ConnectionStringBuilder.BuildPostgresConnectionString(Options));

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            using var reader = await ExecuteTaskWithMetrics(
                connection.QueryMultipleAsync(
                    new CommandDefinition(
                        GetFunctionQuery(parameters, functionName), 
                        parameters, 
                        commandTimeout: timeout, 
                        cancellationToken: cancellationToken)),
                _metrics.CreateMetricsTimerWithLogging(
                    metricEntity,
                    functionName,
                    new { Action = functionName, Parameters = parameters },
                    longTimeRequest,
                    QueryMetricTypeLabel));

            using (_metrics.CreateMetricsTimerWithLogging(
                       metricEntity,
                       functionName,
                       new { Action = functionName, Parameters = parameters },
                       longTimeRequest,
                       ReadMetricTypeLabel))
            using (var convertDataTimer = _metrics.CreateMetricsTimerWithDelayedLogging(
                       metricEntity,
                       functionName,
                       new { Action = functionName, Parameters = parameters },
                       longTimeRequest,
                       ConvertDataMetricTypeLabel))
            {
                var result = await convertData(reader, convertDataTimer);
                return new OperationResult<T>(result);
            }
        }
        catch (Exception e)
        {
            LogWithParameters(e, functionName, metricEntity, parameters);
            return new OperationResult<T>(e);
        }
    }

    public async Task<OperationResult<List<T>>> ExecuteFunctionListAsync<T>(
        string functionName,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var _ = _metrics.CreateMetricsTimerWithLogging(
                metricEntity,
                functionName,
                new { StoredProcedure = functionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            var timeout = timeoutInSeconds ?? GetTimeOut(functionName);
            await using var connection = new NpgsqlConnection(ConnectionStringBuilder.BuildPostgresConnectionString(Options));

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            var result = await ExecuteTaskWithMetrics(
                connection.QueryAsync<T>(
                    new CommandDefinition(
                        GetFunctionQuery(parameters, functionName), 
                        parameters, 
                        commandTimeout: timeout, 
                        cancellationToken: cancellationToken)),
                _metrics.CreateMetricsTimerWithLogging(
                    metricEntity,
                    functionName,
                    new { Action = functionName, Parameters = parameters },
                    longTimeRequest,
                    QueryMetricTypeLabel));

            return new OperationResult<List<T>>(result.AsList());
        }
        catch (Exception e)
        {
            LogWithParameters(e, functionName, metricEntity, parameters);
            return new OperationResult<List<T>>(e);
        }
    }

    public async Task<OperationResult<List<T>>> ExecuteFunctionListAsync<T>(
        string functionName,
        DynamicParameters parameters,
        Func<SqlMapper.GridReader, Task<List<T>>> convertData,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var _ = _metrics.CreateMetricsTimerWithLogging(
                metricEntity,
                functionName,
                new { Action = functionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            var timeout = timeoutInSeconds ?? GetTimeOut(functionName);
            await using var connection = new NpgsqlConnection(ConnectionStringBuilder.BuildPostgresConnectionString(Options));

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            using var reader = await ExecuteTaskWithMetrics(
                connection.QueryMultipleAsync(
                    new CommandDefinition(
                        GetFunctionQuery(parameters, functionName), 
                        parameters, 
                        commandTimeout: timeout, 
                        cancellationToken: cancellationToken)),
                _metrics.CreateMetricsTimerWithLogging(
                    metricEntity,
                    functionName,
                    new { Action = functionName, Parameters = parameters },
                    longTimeRequest,
                    QueryMetricTypeLabel));

            using var disposable = _metrics.CreateMetricsTimerWithLogging(
                metricEntity,
                functionName,
                new { Action = functionName, Parameters = parameters },
                longTimeRequest,
                ReadMetricTypeLabel);
            var result = await convertData(reader);
            return new OperationResult<List<T>>(result);
        }
        catch (Exception e)
        {
            LogWithParameters(e, functionName, metricEntity, parameters);
            return new OperationResult<List<T>>(e);
        }
    }

    public async Task<OperationResult<List<T>>> ExecuteFunctionListAsync<T>(
        string functionName,
        DynamicParameters parameters,
        Func<SqlMapper.GridReader, MetricsTimer, Task<List<T>>> convertData,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var _ = _metrics.CreateMetricsTimerWithLogging(
                metricEntity,
                functionName,
                new { Action = functionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            var timeout = timeoutInSeconds ?? GetTimeOut(functionName);
            await using var connection = new NpgsqlConnection(ConnectionStringBuilder.BuildPostgresConnectionString(Options));

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            using var reader = await ExecuteTaskWithMetrics(
                connection.QueryMultipleAsync(
                    new CommandDefinition(
                        GetFunctionQuery(parameters, functionName), 
                        parameters, 
                        commandTimeout: timeout, 
                        cancellationToken: cancellationToken)),
                _metrics.CreateMetricsTimerWithLogging(
                    metricEntity,
                    functionName,
                    new { Action = functionName, Parameters = parameters },
                    longTimeRequest,
                    QueryMetricTypeLabel));

            using (_metrics.CreateMetricsTimerWithLogging(
                       metricEntity,
                       functionName,
                       new { Action = functionName, Parameters = parameters },
                       longTimeRequest,
                       ReadMetricTypeLabel))
            using (var convertDataTimer = _metrics.CreateMetricsTimerWithDelayedLogging(
                       metricEntity,
                       functionName,
                       new { Action = functionName, Parameters = parameters },
                       longTimeRequest,
                       ConvertDataMetricTypeLabel))
            {
                var result = await convertData(reader, convertDataTimer);
                return new OperationResult<List<T>>(result);
            }
        }
        catch (Exception e)
        {
            LogWithParameters(e, functionName, metricEntity, parameters);
            return new OperationResult<List<T>>(e);
        }
    }

    public async Task<OperationResult<Dictionary<TKey, TValue>>> ExecuteScalarDictionaryFunctionAsync<TKey, TValue>(
        string functionName,
        Func<dynamic, TKey> keySelector,
        Func<dynamic, TValue> valueSelector,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var _ = _metrics.CreateMetricsTimerWithLogging(
                metricEntity,
                functionName,
                new { Action = functionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            var timeout = timeoutInSeconds ?? GetTimeOut(functionName);
            await using var connection = new NpgsqlConnection(ConnectionStringBuilder.BuildPostgresConnectionString(Options));

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            var result = await ExecuteTaskWithMetrics(
                connection.QueryAsync<dynamic>(
                    new CommandDefinition(
                        GetFunctionQuery(parameters, functionName), 
                        parameters, 
                        commandTimeout: timeout, 
                        cancellationToken: cancellationToken)),
                _metrics.CreateMetricsTimerWithLogging(
                    metricEntity,
                    functionName,
                    new { Action = functionName, Parameters = parameters },
                    longTimeRequest,
                    QueryMetricTypeLabel));

            return new OperationResult<Dictionary<TKey, TValue>>(
                result.ToDictionary(keySelector, valueSelector));
        }
        catch (Exception e)
        {
            LogWithParameters(e, functionName, metricEntity, parameters);
            return new OperationResult<Dictionary<TKey, TValue>>(e);
        }
    }

    public async Task<OperationResult<List<T>>> ExecuteRawSqlAsync<T>(
        string sql,
        string queryName,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null, 
        int? timeoutInSeconds = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var _ = _metrics.CreateMetricsTimerWithLogging(
                metricEntity,
                queryName,
                new { Action = queryName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            var timeout = timeoutInSeconds ?? Options.Timeout.Seconds;
            await using var connection = new NpgsqlConnection(ConnectionStringBuilder.BuildPostgresConnectionString(Options));

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            var result = await ExecuteTaskWithMetrics(
                connection.QueryAsync<T>(
                    new CommandDefinition(
                        sql, 
                        parameters, 
                        commandTimeout: timeout, 
                        cancellationToken: cancellationToken)),
                _metrics.CreateMetricsTimerWithLogging(
                    metricEntity,
                    queryName,
                    new { Action = queryName, Parameters = parameters },
                    longTimeRequest,
                    QueryMetricTypeLabel));

            return new OperationResult<List<T>>(result.AsList());
        }
        catch (Exception e)
        {
            LogWithParameters(e, queryName, metricEntity, parameters);
            return new OperationResult<List<T>>(e);
        }
    }

    private string GetFunctionQuery(DynamicParameters @params, string functionName)
        => GetQuery(@params, functionName, FunctionQuery);

    private string GetProcedureQuery(DynamicParameters @params, string procedureName)
        => GetQuery(@params, procedureName, ProcedureQuery);

    private string GetQuery(DynamicParameters @params, string functionName, string querySpecialization)
    {
        var stringBuilder = new StringBuilder($@"{querySpecialization} {functionName}(");
        foreach (var name in @params.ParameterNames)
        {
            stringBuilder.Append($"@{name}, ");
        }

        return stringBuilder.ToString().TrimEnd(' ', ',') + ");";
    }

    private int GetTimeOut(string procedureName)
    {
        return Options.TimeoutDictionary.TryGetValue(procedureName, out var tempTimeout)
            ? tempTimeout
            : Options.Timeout.Seconds;
    }

    private void LogWithParameters(Exception e, string procedureName, string metricEntity, DynamicParameters parameters)
    {
        var parametersWithValues = GetProcedureParametersWithValues(parameters);
        _logger.ErrorWithObject(e, new { procedureName, parameters = parametersWithValues, metricEntity });
    }

    private static Dictionary<string, object> GetProcedureParametersWithValues(DynamicParameters parameters)
    {
        if (parameters == null || parameters.ParameterNames == null || !parameters.ParameterNames.Any())
            return null;

        var paramsArray = new Dictionary<string, object>(parameters.ParameterNames.Count());

        foreach (var name in parameters.ParameterNames)
        {
            var pValue = parameters.Get<dynamic>(name);
            if (pValue is SqlMapper.ICustomQueryParameter customParameter)
            {
                try
                {
                    var dataProperty = customParameter.GetType()
                        .GetFields(BindingFlags.NonPublic | BindingFlags.Instance).First(p => p.Name == "_value");
                    
                    var dataPropertyValue = dataProperty.GetValue(customParameter);

                    paramsArray.Add(name, dataPropertyValue);
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

    private async Task<T> ExecuteTaskWithMetrics<T>(Task<T> task, IDisposable metrics)
    {
        using var _ = metrics;
        return await task;
    }

    private void LoggOnNotice(object sender, NpgsqlNoticeEventArgs e)
    {
        _logger.Warn(e.Notice.MessageText);
    }
}