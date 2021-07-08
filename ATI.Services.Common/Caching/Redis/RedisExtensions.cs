using ATI.Services.Common.Extensions;
using ATI.Services.Common.Initializers;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Common.Caching.Redis
{
    public static class RedisExtensions
    {
        [PublicAPI]
        public static void AddRedis(this IServiceCollection services)
        {
            services.ConfigureByName<CacheManagerOptions>();
            services.AddSingleton<RedisProvider>();
            services.AddTransient<RedisInitializer>();
        }
    }
}