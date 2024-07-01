using System;
using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.Initializers.Interfaces;
using JetBrains.Annotations;
using NLog;

namespace ATI.Services.Common.Initializers;

public class StartupInitializer(IServiceProvider serviceProvider)
{
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    [UsedImplicitly]
    public async Task InitializeAsync()
    {
        var initializers =
            AppDomain.CurrentDomain.GetAssemblies()
                     .SelectMany(assembly => assembly.GetTypes())
                     .Where(type => !type.IsInterface && typeof(IInitializer).IsAssignableFrom(type))
                     .Select(initializer =>
                                 new
                                 {
                                     InitializerType = initializer,
                                     Order = (initializer.GetCustomAttributes(typeof(InitializeOrderAttribute), true)
                                                         .FirstOrDefault() as InitializeOrderAttribute)?.Order ??
                                             InitializeOrder.Last
                                 }
                     )
                     .ToList();

        foreach (var initializerInfo in initializers.OrderBy(i => i.Order))
        {
            if (serviceProvider.GetService(initializerInfo.InitializerType) is not IInitializer initializer)
                continue;

            Console.WriteLine(initializer.InitStartConsoleMessage());
                
            var initTimeoutAttribute =
                initializer.GetType().GetCustomAttributes(typeof(InitializeTimeoutAttribute), false)
                           .FirstOrDefault() as InitializeTimeoutAttribute;

                
                
            var initTask = initializer.InitializeAsync();
            if (initTimeoutAttribute is not null)
            {
                var initTimeout = TimeSpan.FromSeconds(initTimeoutAttribute.InitTimeoutSec);
                await Task.WhenAny(initTask, Task.Delay(initTimeout));
                if (!initTask.IsCompleted)
                {
                    if (initTimeoutAttribute.Required)
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
            }
            else
            {
                await initTask;
            }
                
            Console.WriteLine(initializer.InitEndConsoleMessage());
            _logger.Trace($"{initializer.GetType()} initialized");
        }
    }
}