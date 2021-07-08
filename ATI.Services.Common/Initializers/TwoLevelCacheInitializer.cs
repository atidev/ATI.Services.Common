using System.Threading.Tasks;
using ATI.Services.Common.Caching.MemoryCaching;
using ATI.Services.Common.Initializers.Interfaces;
using JetBrains.Annotations;

namespace ATI.Services.Common.Initializers
{
    [UsedImplicitly]
    [InitializeOrder(Order = InitializeOrder.Third)]
    public class TwoLevelCacheInitializer : IInitializer
    {
        private static bool _initialized;

        private readonly TwoLevelCacheProvider _twoLevelCacheProvider;

        public TwoLevelCacheInitializer(TwoLevelCacheProvider twoLevelCacheProvider)
        {
            _twoLevelCacheProvider = twoLevelCacheProvider;
        }

        public async Task InitializeAsync()
        {
            if (_initialized)
            {
                return;
            }
            
            await _twoLevelCacheProvider.InitAsync();
            _initialized = true;
        }
    }
}