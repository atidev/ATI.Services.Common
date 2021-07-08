using System.Threading.Tasks;
using ATI.Services.Common.Caching.Redis;
using ATI.Services.Common.Initializers.Interfaces;
using JetBrains.Annotations;

namespace ATI.Services.Common.Initializers
{
    [UsedImplicitly]
    [InitializeOrder(Order = InitializeOrder.Third)]
    public class RedisInitializer : IInitializer
    {
        private static bool _initialized;
        private readonly RedisProvider _redisProvider;

        public RedisInitializer(RedisProvider redisProvider)
        {
            _redisProvider = redisProvider;
        } 

        public async Task InitializeAsync()
        {
            if (_initialized)
            {
                return;
            }
            
            await _redisProvider.InitAsync();
            _initialized = true;
        }
    }
}