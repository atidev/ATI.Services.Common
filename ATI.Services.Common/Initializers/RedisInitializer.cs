using System.Threading.Tasks;
using ATI.Services.Common.Caching.Redis;
using ATI.Services.Common.Caching.Redis.Abstractions;
using ATI.Services.Common.Initializers.Interfaces;
using JetBrains.Annotations;

namespace ATI.Services.Common.Initializers
{
    [UsedImplicitly]
    [InitializeOrder(Order = InitializeOrder.Third)]
    public class RedisInitializer : IInitializer
    {
        private static bool _initialized;
        private readonly IRedisProvider _redisProvider;

        public RedisInitializer(IRedisProvider redisProvider)
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
        
        public string InitStartConsoleMessage()
        {
            return "Start Redis initializer";
        }

        public string InitEndConsoleMessage()
        {
            return $"End Redis initializer, result {_initialized}";
        }
    }
}