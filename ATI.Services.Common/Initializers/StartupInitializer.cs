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
                if (_serviceProvider.GetService(initializerInfo.InitializerType) is not IInitializer initializer)
                    continue;

                Console.WriteLine(initializer.InitStartConsoleMessage());
                
                var initTimeoutAttribute =
                    initializer.GetType().GetCustomAttributes(typeof(InitializeTimeoutAttribute), false)
                               .FirstOrDefault() as InitializeTimeoutAttribute;
                
                var initTask = initializer.InitializeAsync();
                var initTimeout = initTimeoutAttribute?.InitTimeout ?? TimeSpan.FromSeconds(30);
                var required = initTimeoutAttribute?.Required ?? false;
                    
                await Task.WhenAny(initTask, Task.Delay(initTimeout));
                if (!initTask.IsCompleted) 
                {
                    if (required)
                    {
                        var message = $"Required initializer {initializer.GetType().Name} didn't complete in {initTimeout}";
                        _logger.Error(message);
                        throw new Exception(message);
                    }

                    var timeoutMessage = $"Initializer {initializer.GetType().Name} didn't complete in {initTimeout}, continue in background.";
                    Console.WriteLine(timeoutMessage);
                    _logger.Warn(timeoutMessage);
                    continue;
                }

                Console.WriteLine(initializer.InitEndConsoleMessage());
                _logger.Trace($"{initializer.GetType()} initialized");
            }
        }
    }
}