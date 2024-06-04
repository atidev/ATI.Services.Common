using System.Collections.Concurrent;
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
    private readonly ConcurrentDictionary<string, PostgresDapper> _configuredDataBases = new();

    public PostgresDapperProvider(IOptionsMonitor<DbManagerOptions> dbManagerOptions, MetricsFactory metricsFactory)
    {
        foreach (var kvDataBaseOptions in dbManagerOptions.CurrentValue.DataBaseOptions)
        {
            _configuredDataBases.TryAdd(kvDataBaseOptions.Key, new PostgresDapper(kvDataBaseOptions.Value, metricsFactory));
        }
        
        dbManagerOptions.OnChange(o => ReloadDatabases(o.DataBaseOptions, metricsFactory));
    }
    
    public PostgresDapper GetDb(string dbName)
    {
        var isDbConfigured = _configuredDataBases.TryGetValue(dbName, out var db);
        if (isDbConfigured)
            return db;

        _logger.Error($"No {dbName} database was configured");
        return null;
    }

    private void ReloadDatabases(Dictionary<string, DataBaseOptions> newDataBaseOptions, MetricsFactory metricsFactory)
    {
        foreach (var (dbName, newDbOptions) in newDataBaseOptions)
        {
            if (_configuredDataBases.TryGetValue(dbName, out var oldDbOptions))
            {
                //Если кто-то сменил change token IOptionsMonitor, и не сменил креды от бд, не нужно сбрасывать коннекшен пул
                if (oldDbOptions.Options.UserName == newDbOptions.UserName 
                    && oldDbOptions.Options.Password == newDbOptions.Password)
                    continue;
                
                // Удаляем старые коннекты при смене коннекшн стринга, так как у постгрес имеется лимит на количество открытых соединений
                NpgsqlConnection.ClearPool(new NpgsqlConnection(ConnectionStringBuilder.BuildPostgresConnectionString(oldDbOptions.Options)));
                oldDbOptions.Options = newDbOptions;
                
            }
            else
                _configuredDataBases.TryAdd(dbName, new PostgresDapper(newDbOptions, metricsFactory));
        }
    }
}
