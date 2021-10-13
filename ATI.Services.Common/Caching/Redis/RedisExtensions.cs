using ATI.Services.Common.Extensions;
using ATI.Services.Common.Initializers;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace ATI.Services.Common.Caching.Redis
{
    public static class RedisExtensions
    {
        [PublicAPI]
        public static void AddRedis(this IServiceCollection services)
        {
            ConnectionMultiplexer.SetFeatureFlag("preventthreadtheft", true);
            services.ConfigureByName<CacheManagerOptions>();
            services.AddSingleton<RedisProvider>();
            services.AddTransient<RedisInitializer>();
        }
    }
}