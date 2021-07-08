using System;
using System.Collections.Generic;
using Microsoft.Extensions.Caching.Memory;

namespace ATI.Services.Common.Caching.MemoryCaching
{
    public class MemoryCacheOptions
    {
        public Dictionary<string, MemoryOptions> CacheOptions { get; set; } = new Dictionary<string, MemoryOptions>();
        public MemoryCacheEntryOptions DefaultOptions { get; set; } = new MemoryCacheEntryOptions{ SlidingExpiration = TimeSpan.FromHours(2)};
    }
}