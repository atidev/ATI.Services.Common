using ATI.Services.Common.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Common.Sql;

public static class SqlExtensions
{
    [UsedImplicitly]
    public static void AddSql(this IServiceCollection services, DataBases db = DataBases.MsSql)
    {
            services.ConfigureByName<DbManagerOptions>();
            if (db.HasFlag(DataBases.MsSql))
                services.AddSingleton<DbProvider>();
            if (db.HasFlag(DataBases.Postgresql))
                services.AddSingleton<PostgresDapperProvider>();
    }
}