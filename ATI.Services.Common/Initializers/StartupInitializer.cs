using System;
using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.Initializers.Interfaces;
using JetBrains.Annotations;
using NLog;

namespace ATI.Services.Common.Initializers
{
    public class StartupInitializer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public StartupInitializer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [UsedImplicitly]
        public async Task InitializeAsync()
        {
            var initializers = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => !type.IsInterface && typeof(IInitializer).IsAssignableFrom(type))
                .Select(initializer =>
                    new
                    {
                        InitializerType = initializer,
                        Order = (initializer.GetCustomAttributes(typeof(InitializeOrderAttribute), true)
                            .FirstOrDefault() as InitializeOrderAttribute)?.Order ?? InitializeOrder.Last
                    }
                )
                .ToList();

            foreach (var initializerInfo in initializers.OrderBy(i => i.Order))
            {
                if (_serviceProvider.GetService(initializerInfo.InitializerType) is IInitializer initializer)
                {
                    await initializer.InitializeAsync();
                    _logger.Trace($"{initializer.GetType()} initialized");
                }
            }
        }
    }
}