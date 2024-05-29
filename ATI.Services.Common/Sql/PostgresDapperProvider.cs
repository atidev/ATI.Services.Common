using System.Collections.Concurrent;
using System.Collections.Generic;
using ATI.Services.Common.Logging;
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
    private readonly ConcurrentDictionary<string, PostgresDapper> _configuredDataBases = new();

    public PostgresDapperProvider(IOptionsMonitor<DbManagerOptions> dbManagerOptions, MetricsFactory metricsFactory)
    {
        foreach (var kvDataBaseOptions in dbManagerOptions.CurrentValue.DataBaseOptions)
        {
            _configuredDataBases.TryAdd(kvDataBaseOptions.Key, new PostgresDapper(kvDataBaseOptions.Value, metricsFactory));
        }
        
        dbManagerOptions.OnChange(o => GetConfiguredDataBases(o.DataBaseOptions, metricsFactory));
    }

    private void GetConfiguredDataBases(Dictionary<string, DataBaseOptions> newDataBaseOptions, MetricsFactory metricsFactory)
    {
        _logger.WarnWithObject("New database options", new { newDataBaseOptions });
        
        foreach (var (newPostgresName, newDataBaseOption) in newDataBaseOptions)
        {
            if (_configuredDataBases.TryGetValue(newPostgresName, out var oldDatabase))
            {
                //Если кто-то сменил change token IOptionsMonitor, и не сменил креды от бд, не нужно сбрасывать коннекшен пул
                if (oldDatabase.Options.UserName == newDataBaseOption.UserName 
                    && oldDatabase.Options.Password == newDataBaseOption.Password)
                    continue;
                
                _logger.WarnWithObject("Rotate postgres credentials on common", new
                {
                    RotateOptions = newDataBaseOption, 
                    oldDatabase.Options,
                });
                NpgsqlConnection.ClearPool(new NpgsqlConnection(ConnectionStringBuilder.BuildPostgresConnectionString(oldDatabase.Options)));
                oldDatabase.Options = newDataBaseOption;
                _logger.WarnWithObject("Postgres credentials after rotate", new
                {
                    RotateOptions = newDataBaseOption, 
                    oldDatabase.Options,
                });
            }
            else
                _configuredDataBases.TryAdd(newPostgresName, new PostgresDapper(newDataBaseOption, metricsFactory));
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
