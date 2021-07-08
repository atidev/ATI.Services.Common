using ATI.Services.Common.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Common.Sql
{
    public static class SqlExtensions
    {
        [UsedImplicitly]
        public static void AddSql(this IServiceCollection services)
        {
            services.ConfigureByName<DbManagerOptions>();
            services.AddSingleton<DbProvider>();
        }
    }
}