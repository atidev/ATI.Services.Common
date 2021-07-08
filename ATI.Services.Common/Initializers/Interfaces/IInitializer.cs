using System.Threading.Tasks;

namespace ATI.Services.Common.Initializers.Interfaces
{
    /// <summary>
    /// Интерфейс который маркирует объект, как требующий инициализации на старте приложения
    /// используется классом <see cref="StartupInitializer"/>
    /// для задания порядка инициализации используйте аттрибут <see cref="InitializeOrderAttribute"/>
    /// Имеющийся порядок на данный момент:
    /// ATI.Services.Authorization.AuthorizationInitializer - InitializeOrder.First
    /// <see cref="Tracing.TracingInitializer"/> -  InitializeOrder.First
    /// <see cref="ServiceVariables.ServiceVariablesInitializer"/> -  InitializeOrder.First
    /// <see cref="MetricsInitializer"/> -  InitializeOrder.Second
    /// <see cref="RedisInitializer"/> -  InitializeOrder.Third
    /// <see cref="TwoLevelCacheInitializer"/> -  InitializeOrder.Third
    /// <see cref="Caching.LocalCache.LocalCache{T}"/> -  InitializeOrder.Fourth
    /// ATI.Services.Consul.ConsulInitializer -  InitializeOrder.Sixth
    /// </summary>
    public interface IInitializer
    { 
        Task InitializeAsync();
    }
}