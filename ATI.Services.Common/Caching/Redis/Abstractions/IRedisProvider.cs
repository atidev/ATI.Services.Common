using System.Collections.Generic;
using System.Threading.Tasks;

namespace ATI.Services.Common.Caching.Redis.Abstractions;

public interface IRedisProvider
{
    public IRedisCache GetCache(string cacheName);

    public List<IRedisCache> GetAllCaches();
    
    public Task InitAsync();
}