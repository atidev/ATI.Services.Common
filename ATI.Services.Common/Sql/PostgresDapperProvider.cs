using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NLog;

namespace ATI.Services.Common.Sql;

[PublicAPI]
public class PostgresDapperProvider
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<string, PostgresDapper> _configuredDataBases = new();

    public PostgresDapperProvider(IOptions<DbManagerOptions> dbManagerOptions)
    {
        foreach (var kvDataBaseOptions in dbManagerOptions.Value.DataBaseOptions)
        {
            _configuredDataBases.Add(kvDataBaseOptions.Key, new PostgresDapper(kvDataBaseOptions.Value));
        }
    }

    public PostgresDapper GetDb(string dbName)
    {
        var isDbConfigured = _configuredDataBases.TryGetValue(dbName, out var db);
        if (isDbConfigured)
            return db;

        _logger.Error($"Тo {dbName} database was configured");
        return null;
    }
}
