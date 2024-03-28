using System.Collections.Generic;
using ATI.Services.Common.Metrics;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NLog;

namespace ATI.Services.Common.Sql
{
    [PublicAPI]
    public class DbProvider
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<string, DapperDb> _configuredDataBases = new();

        public DbProvider(IOptions<DbManagerOptions> dbManagerOptions, MetricsFactory metricsFactory)
        {
            foreach (var kvDataBaseOptions in dbManagerOptions.Value.DataBaseOptions)
            {
                _configuredDataBases.Add(kvDataBaseOptions.Key, new DapperDb(kvDataBaseOptions.Value, metricsFactory));
            }
        }

        public DapperDb GetDb(string dbName)
        {
            var isDbConfigured = _configuredDataBases.TryGetValue(dbName, out var db);
            if (isDbConfigured)
            {
                return db;
            }
            _logger.Error($"В пуле нет базы {dbName}");
            return null;
        }
    }
}
