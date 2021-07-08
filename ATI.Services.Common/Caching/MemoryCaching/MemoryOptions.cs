using Microsoft.Extensions.Caching.Memory;

namespace ATI.Services.Common.Caching.MemoryCaching
{
    public class MemoryOptions
    {
        public MemoryCacheEntryOptions SetsCacheOptions { get; set; }
        public MemoryCacheEntryOptions EntityCacheOptions { get; set; }
    }
}