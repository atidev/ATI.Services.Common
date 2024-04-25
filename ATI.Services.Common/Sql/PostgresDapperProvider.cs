using System.Collections.Generic;
using ATI.Services.Common.Metrics;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NLog;
using Npgsql;

namespace ATI.Services.Common.Sql;

[PublicAPI]
public class PostgresDapperProvider
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<string, PostgresDapper> _configuredDataBases = new();

    public PostgresDapperProvider(IOptionsMonitor<DbManagerOptions> dbManagerOptions, MetricsFactory metricsFactory)
    {
        foreach (var kvDataBaseOptions in dbManagerOptions.CurrentValue.DataBaseOptions)
        {
            _configuredDataBases.Add(kvDataBaseOptions.Key, new PostgresDapper(kvDataBaseOptions.Value, metricsFactory));
        }
        
        dbManagerOptions.OnChange(o => GetConfiguredDataBases(o.DataBaseOptions, metricsFactory));
    }

    private void GetConfiguredDataBases(Dictionary<string, DataBaseOptions> dataBaseOptions, MetricsFactory metricsFactory)
    {
        foreach (var kvDataBaseOptions in dataBaseOptions)
        {
            if (_configuredDataBases.TryGetValue(kvDataBaseOptions.Key, out var config))
            {
                NpgsqlConnection.ClearPool(new NpgsqlConnection(ConnectionStringBuilder.BuildPostgresConnectionString(_configuredDataBases[kvDataBaseOptions.Key].Options)));
                _configuredDataBases[kvDataBaseOptions.Key].Options = kvDataBaseOptions.Value;
                
            }
            else
                _configuredDataBases.Add(kvDataBaseOptions.Key, new PostgresDapper(kvDataBaseOptions.Value, metricsFactory));
        }
    }

    
    public PostgresDapper GetDb(string dbName)
    {
        var isDbConfigured = _configuredDataBases.TryGetValue(dbName, out var db);
        if (isDbConfigured)
            return db;

        _logger.Error($"No {dbName} database was configured");
        return null;
    }
}
