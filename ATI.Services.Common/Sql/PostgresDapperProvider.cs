using System.Collections.Generic;
using ATI.Services.Common.Metrics;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NLog;

namespace ATI.Services.Common.Sql;

[PublicAPI]
public class PostgresDapperProvider
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<string, PostgresDapper> _configuredDataBases = new();

    public PostgresDapperProvider(IOptions<DbManagerOptions> dbManagerOptions, MetricsFactory metricsFactory)
    {
        foreach (var kvDataBaseOptions in dbManagerOptions.Value.DataBaseOptions)
        {
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
