using ATI.Services.Common.Extensions;
using ATI.Services.Common.Initializers;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Common.Caching.MemoryCaching
{
    public static class TwoLevelCacheExtensions
    {
        [UsedImplicitly]
        public static void AddTwoLevelCache(this IServiceCollection services)
        {
            services.ConfigureByName<MemoryCacheOptions>();
            services.AddMemoryCache();
            services.AddSingleton<TwoLevelCacheProvider>();
            services.AddTransient<TwoLevelCacheInitializer>();
        }
    }
}

