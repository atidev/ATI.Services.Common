using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Metrics;
using Dapper;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NLog;
using Npgsql;

namespace ATI.Services.Common.Sql;

[PublicAPI]
public class PostgresDapper
{
    private readonly DataBaseOptions _options;
    private readonly MetricsFactory _metricsFactory;
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private const string ReadMetricTypeLabel = "read";
    private const string ConvertDataMetricTypeLabel = "convert";
    private const string QueryMetricTypeLabel = "query";
    private const string FullMetricTypeLabel = "full";

    private const string FunctionQuery = "SELECT";
    private const string ProcedureQuery = "CALL";

    public PostgresDapper(DataBaseOptions options)
    {
        _options = options;
        _metricsFactory = MetricsFactory.CreateSqlMetricsFactory(nameof(DapperDb), _options.LongTimeRequest, "type");
    }

    public async Task<OperationResult> ExecuteFunctionAsync(
        string functionName,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        return await ExecuteAsync(
            functionName,
            GetFunctionQuery(parameters, functionName),
            parameters,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds);
    }

    public async Task<OperationResult> ExecuteProcedureAsync(
        string procedureName,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        return await ExecuteAsync(
            procedureName,
            GetProcedureQuery(parameters, procedureName),
            parameters,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds);
    }

    private async Task<OperationResult> ExecuteAsync(
        string actionName,
        string query,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        try
        {
            using var _ = _metricsFactory.CreateMetricsTimerWithLogging(
                metricEntity,
                actionName,
                new { Action = actionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            await using var connection = new NpgsqlConnection(_options.ConnectionString);

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            var timeout = timeoutInSeconds ?? GetTimeOut(actionName);

            using var disposable = _metricsFactory.CreateMetricsTimerWithLogging(
                metricEntity,
                actionName,
                new { Action = actionName, Parameters = parameters },
                longTimeRequest, QueryMetricTypeLabel);

            await connection.ExecuteAsync(
                query,
                parameters,
                commandTimeout: timeout);
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
        int? timeoutInSeconds = null)
    {
        return await ExecuteObjectAsync<T>(
            functionName,
            GetFunctionQuery(parameters, functionName),
            parameters,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds
        );
    }

    public async Task<OperationResult<T>> ExecuteProcedureObjectAsync<T>(
        string procedureName,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        return await ExecuteObjectAsync<T>(
            procedureName,
            GetProcedureQuery(parameters, procedureName),
            parameters,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds
        );
    }

    private async Task<OperationResult<T>> ExecuteObjectAsync<T>(
        string actionName,
        string query,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        try
        {
            using var _ = _metricsFactory.CreateMetricsTimerWithLogging(
                metricEntity,
                actionName,
                new { StoredProcedure = actionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            var timeout = timeoutInSeconds ?? GetTimeOut(actionName);
            await using var connection = new NpgsqlConnection(_options.ConnectionString);

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            var result = await ExecuteTaskWithMetrics(connection.ExecuteScalarAsync<T>(
                    query,
                    parameters,
                    commandTimeout: timeout),
                _metricsFactory.CreateMetricsTimerWithLogging(
                    metricEntity,
                    actionName,
                    new { Action = actionName, Parameters = parameters },
                    longTimeRequest,
                    QueryMetricTypeLabel));

            return new OperationResult<T>(result);
        }
        catch (Exception e)
        {
            LogWithParameters(e, actionName, metricEntity, parameters);
            return new OperationResult<T>(e);
        }
    }

    #region objects' sets // TODO: remove

    public async Task<OperationResult<T>> ExecuteFunctionObjectAsync<T>(
        string functionName,
        DynamicParameters parameters,
        Func<SqlMapper.GridReader, Task<T>> convertData,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        return await ExecuteObjectAsync<T>(
            functionName,
            GetFunctionQuery(parameters, functionName),
            parameters,
            convertData,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds);
    }

    public async Task<OperationResult<T>> ExecuteProcedureObjectAsync<T>(
        string procedureName,
        DynamicParameters parameters,
        Func<SqlMapper.GridReader, Task<T>> convertData,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        return await ExecuteObjectAsync<T>(
            procedureName,
            GetProcedureQuery(parameters, procedureName),
            parameters,
            convertData,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds);
    }

    private async Task<OperationResult<T>> ExecuteObjectAsync<T>(
        string actionName,
        string query,
        DynamicParameters parameters,
        Func<SqlMapper.GridReader, Task<T>> convertData,
        string metricEntity,
        bool receiveNotice,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        try
        {
            using var _ = _metricsFactory.CreateMetricsTimerWithLogging(
                metricEntity,
                actionName,
                new { Action = actionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            var timeout = timeoutInSeconds ?? GetTimeOut(actionName);

            await using var connection = new NpgsqlConnection(_options.ConnectionString);

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            using var reader = await ExecuteTaskWithMetrics(
                connection.QueryMultipleAsync(
                    query,
                    parameters,
                    commandTimeout: timeout),
                _metricsFactory.CreateMetricsTimerWithLogging(
                    metricEntity,
                    actionName,
                    new { Action = actionName, Parameters = parameters },
                    longTimeRequest,
                    QueryMetricTypeLabel));

            using var disposable = _metricsFactory.CreateMetricsTimerWithLogging(
                metricEntity,
                actionName,
                new { Action = actionName, Parameters = parameters },
                longTimeRequest,
                ReadMetricTypeLabel);

            var result = await convertData(reader);
            return new OperationResult<T>(result);
        }
        catch (Exception e)
        {
            LogWithParameters(e, actionName, metricEntity, parameters);
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
        int? timeoutInSeconds = null)
    {
        return await ExecuteObjectAsync(
            functionName,
            GetFunctionQuery(parameters, functionName),
            parameters,
            convertData,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds);
    }

    public async Task<OperationResult<T>> ExecuteProcedureObjectAsync<T>(
        string procedureName,
        DynamicParameters parameters,
        Func<SqlMapper.GridReader, MetricsTimer, Task<T>> convertData,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        return await ExecuteObjectAsync(
            procedureName,
            GetProcedureQuery(parameters, procedureName),
            parameters,
            convertData,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds);
    }

    private async Task<OperationResult<T>> ExecuteObjectAsync<T>(
        string actionName,
        string query,
        DynamicParameters parameters,
        Func<SqlMapper.GridReader, MetricsTimer, Task<T>> convertData,
        string metricEntity,
        bool receiveNotice,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        try
        {
            using var _ = _metricsFactory.CreateMetricsTimerWithLogging(
                metricEntity,
                actionName,
                new { Action = actionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            var timeout = timeoutInSeconds ?? GetTimeOut(actionName);
            await using var connection = new NpgsqlConnection(_options.ConnectionString);

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            using var reader = await ExecuteTaskWithMetrics(
                connection.QueryMultipleAsync(
                    query,
                    parameters,
                    commandTimeout: timeout),
                _metricsFactory.CreateMetricsTimerWithLogging(
                    metricEntity,
                    actionName,
                    new { Action = actionName, Parameters = parameters },
                    longTimeRequest,
                    QueryMetricTypeLabel));

            using (_metricsFactory.CreateMetricsTimerWithLogging(
                       metricEntity,
                       actionName,
                       new { Action = actionName, Parameters = parameters },
                       longTimeRequest,
                       ReadMetricTypeLabel))
            using (var convertDataTimer = _metricsFactory.CreateMetricsTimerWithDelayedLogging(
                       metricEntity,
                       actionName,
                       new { Action = actionName, Parameters = parameters },
                       longTimeRequest,
                       ConvertDataMetricTypeLabel))
            {
                var result = await convertData(reader, convertDataTimer);
                return new OperationResult<T>(result);
            }
        }
        catch (Exception e)
        {
            LogWithParameters(e, actionName, metricEntity, parameters);
            return new OperationResult<T>(e);
        }
    }

    #endregion

    #region lists

    public async Task<OperationResult<List<T>>> ExecuteFunctionListAsync<T>(
        string functionName,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        return await ExecuteListAsync<T>(
            functionName,
            GetFunctionQuery(parameters, functionName),
            parameters,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds);
    }

    public async Task<OperationResult<List<T>>> ExecuteProcedureListAsync<T>(
        string procedureName,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        return await ExecuteListAsync<T>(
            procedureName,
            GetProcedureQuery(parameters, procedureName),
            parameters,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds);
    }

    private async Task<OperationResult<List<T>>> ExecuteListAsync<T>(
        string actionName,
        string query,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        try
        {
            using var _ = _metricsFactory.CreateMetricsTimerWithLogging(
                metricEntity,
                actionName,
                new { StoredProcedure = actionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            var timeout = timeoutInSeconds ?? GetTimeOut(actionName);
            await using var connection = new NpgsqlConnection(_options.ConnectionString);

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            var result = await ExecuteTaskWithMetrics(
                connection.QueryAsync<T>(
                    query,
                    parameters,
                    commandTimeout: timeout),
                _metricsFactory.CreateMetricsTimerWithLogging(
                    metricEntity,
                    actionName,
                    new { Action = actionName, Parameters = parameters },
                    longTimeRequest,
                    QueryMetricTypeLabel));

            return new OperationResult<List<T>>(result.AsList());
        }
        catch (Exception e)
        {
            LogWithParameters(e, actionName, metricEntity, parameters);
            return new OperationResult<List<T>>(e);
        }
    }

    #endregion

    #region lists' sets

    public async Task<OperationResult<List<T>>> ExecuteFunctionListAsync<T>(
        string functionName,
        DynamicParameters parameters,
        Func<SqlMapper.GridReader, Task<List<T>>> convertData,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        return await ExecuteResultSetAsync(
            functionName,
            GetFunctionQuery(parameters, functionName),
            parameters,
            convertData,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds
        );
    }

    public async Task<OperationResult<List<T>>> ExecuteProcedureListAsync<T>(
        string procedureName,
        DynamicParameters parameters,
        Func<SqlMapper.GridReader, Task<List<T>>> convertData,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        return await ExecuteResultSetAsync(
            procedureName,
            GetProcedureQuery(parameters, procedureName),
            parameters,
            convertData,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds
        );
    }

    private async Task<OperationResult<List<T>>> ExecuteResultSetAsync<T>(
        string actionName,
        string query,
        DynamicParameters parameters,
        Func<SqlMapper.GridReader, Task<List<T>>> convertData,
        string metricEntity,
        bool receiveNotice,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        try
        {
            using var _ = _metricsFactory.CreateMetricsTimerWithLogging(
                metricEntity,
                actionName,
                new { Action = actionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            var timeout = timeoutInSeconds ?? GetTimeOut(actionName);
            await using var connection = new NpgsqlConnection(_options.ConnectionString);

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            using var reader = await ExecuteTaskWithMetrics(
                connection.QueryMultipleAsync(
                    query,
                    parameters,
                    commandTimeout: timeout),
                _metricsFactory.CreateMetricsTimerWithLogging(
                    metricEntity,
                    actionName,
                    new { Action = actionName, Parameters = parameters },
                    longTimeRequest,
                    QueryMetricTypeLabel));

            using var disposable = _metricsFactory.CreateMetricsTimerWithLogging(
                metricEntity,
                actionName,
                new { Action = actionName, Parameters = parameters },
                longTimeRequest,
                ReadMetricTypeLabel);
            var result = await convertData(reader);
            return new OperationResult<List<T>>(result);
        }
        catch (Exception e)
        {
            LogWithParameters(e, actionName, metricEntity, parameters);
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
        int? timeoutInSeconds = null)
    {
        return await ExecuteResultSetAsync(
            functionName,
            GetFunctionQuery(parameters, functionName),
            parameters,
            convertData,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds);
    }

    public async Task<OperationResult<List<T>>> ExecuteProcedureListAsync<T>(
        string procedureName,
        DynamicParameters parameters,
        Func<SqlMapper.GridReader, MetricsTimer, Task<List<T>>> convertData,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        return await ExecuteResultSetAsync(
            procedureName,
            GetProcedureQuery(parameters, procedureName),
            parameters,
            convertData,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds);
    }

    private async Task<OperationResult<List<T>>> ExecuteResultSetAsync<T>(
        string actionName,
        string query,
        DynamicParameters parameters,
        Func<SqlMapper.GridReader, MetricsTimer, Task<List<T>>> convertData,
        string metricEntity,
        bool receiveNotice,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        try
        {
            using var _ = _metricsFactory.CreateMetricsTimerWithLogging(
                metricEntity,
                actionName,
                new { Action = actionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            var timeout = timeoutInSeconds ?? GetTimeOut(actionName);
            await using var connection = new NpgsqlConnection(_options.ConnectionString);

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            using var reader = await ExecuteTaskWithMetrics(
                connection.QueryMultipleAsync(
                    query,
                    parameters,
                    commandTimeout: timeout),
                _metricsFactory.CreateMetricsTimerWithLogging(
                    metricEntity,
                    actionName,
                    new { Action = actionName, Parameters = parameters },
                    longTimeRequest,
                    QueryMetricTypeLabel));

            using (_metricsFactory.CreateMetricsTimerWithLogging(
                       metricEntity,
                       actionName,
                       new { Action = actionName, Parameters = parameters },
                       longTimeRequest,
                       ReadMetricTypeLabel))
            using (var convertDataTimer = _metricsFactory.CreateMetricsTimerWithDelayedLogging(
                       metricEntity,
                       actionName,
                       new { Action = actionName, Parameters = parameters },
                       longTimeRequest,
                       ConvertDataMetricTypeLabel))
            {
                var result = await convertData(reader, convertDataTimer);
                return new OperationResult<List<T>>(result);
            }
        }
        catch (Exception e)
        {
            LogWithParameters(e, actionName, metricEntity, parameters);
            return new OperationResult<List<T>>(e);
        }
    }

    #endregion

    #region Dictionary

    public async Task<OperationResult<Dictionary<TKey, TValue>>> ExecuteScalarDictionaryFunctionAsync<TKey, TValue>(
        string functionName,
        Func<dynamic, TKey> keySelector,
        Func<dynamic, TValue> valueSelector,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        return await ExecuteScalarDictionaryAsync(
            functionName,
            GetFunctionQuery(parameters, functionName),
            keySelector,
            valueSelector,
            parameters,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds);
    }

    public async Task<OperationResult<Dictionary<TKey, TValue>>> ExecuteScalarDictionaryProcedureAsync<TKey, TValue>(
        string procedureName,
        Func<dynamic, TKey> keySelector,
        Func<dynamic, TValue> valueSelector,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice = false,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        return await ExecuteScalarDictionaryAsync(
            procedureName,
            GetProcedureQuery(parameters, procedureName),
            keySelector,
            valueSelector,
            parameters,
            metricEntity,
            receiveNotice,
            longTimeRequest,
            timeoutInSeconds);
    }

    private async Task<OperationResult<Dictionary<TKey, TValue>>> ExecuteScalarDictionaryAsync<TKey, TValue>(
        string actionName,
        string query,
        Func<dynamic, TKey> keySelector,
        Func<dynamic, TValue> valueSelector,
        DynamicParameters parameters,
        string metricEntity,
        bool receiveNotice,
        TimeSpan? longTimeRequest = null,
        int? timeoutInSeconds = null)
    {
        try
        {
            using var _ = _metricsFactory.CreateMetricsTimerWithLogging(
                metricEntity,
                actionName,
                new { Action = actionName, Parameters = parameters },
                longTimeRequest,
                FullMetricTypeLabel);

            var timeout = timeoutInSeconds ?? GetTimeOut(actionName);
            await using var connection = new NpgsqlConnection(_options.ConnectionString);

            if (receiveNotice)
                connection.Notice += LoggOnNotice;

            var result = await ExecuteTaskWithMetrics(
                connection.QueryAsync<dynamic>(
                    query,
                    parameters,
                    commandTimeout: timeout),
                _metricsFactory.CreateMetricsTimerWithLogging(
                    metricEntity,
                    actionName,
                    new { Action = actionName, Parameters = parameters },
                    longTimeRequest,
                    QueryMetricTypeLabel));

            return new OperationResult<Dictionary<TKey, TValue>>(
                result.ToDictionary(keySelector, valueSelector));
        }
        catch (Exception e)
        {
            LogWithParameters(e, actionName, metricEntity, parameters);
            return new OperationResult<Dictionary<TKey, TValue>>(e);
        }
    }

    #endregion


    private string GetFunctionQuery(DynamicParameters @params, string functionName)
        => GetQuery(@params, functionName, FunctionQuery);

    private string GetProcedureQuery(DynamicParameters @params, string procedureName)
        => GetQuery(@params, procedureName, ProcedureQuery);

    private string GetQuery(DynamicParameters @params, string functionName, string querySpecialization)
    {
        var stringBuilder = new StringBuilder($"{querySpecialization} {functionName}(");
        foreach (var name in @params.ParameterNames)
        {
            stringBuilder.Append($"@{name}, ");
        }

        return stringBuilder.ToString().TrimEnd(' ', ',') + ");";
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
                        .GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(p => p.Name != "_sqlDataRecord").ToList();

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