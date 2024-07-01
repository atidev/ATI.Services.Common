using System;
using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.Initializers.Interfaces;
using ATI.Services.Common.Logging;
using JetBrains.Annotations;
using NLog;
using Polly;

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
            var initializerName = initializer.GetType().Name;

            if (initializer.GetType().GetCustomAttributes(typeof(InitializeTimeoutAttribute), false).FirstOrDefault()
                is not InitializeTimeoutAttribute initTimeoutAttribute)
            {
                try
                {
                    await initializer.InitializeAsync();
                }
                catch (Exception e)
                {
                    _logger.ErrorWithObject(e, $"Exception during initializer {initializerName}");
                }
            }
            else
            {
                await InitWithPolicy(initTimeoutAttribute, initializerName, () => initializer.InitializeAsync());
                continue;
            }

            Console.WriteLine(initializer.InitEndConsoleMessage());
            _logger.Trace($"{initializerName} initialized");
        }

        return;

        async Task InitWithPolicy(InitializeTimeoutAttribute initTimeoutAttribute,
                                  string initializerName,
                                  Func<Task> init)
        {
            var initPolicy =
                Policy.WrapAsync(Policy.TimeoutAsync(TimeSpan.FromSeconds(initTimeoutAttribute.InitTimeoutSec)),
                                 Policy.Handle<Exception>().WaitAndRetryAsync(
                                     initTimeoutAttribute.Retry,
                                     i => TimeSpan.FromMilliseconds(Math.Min(200 * i, 2000)),
                                     (exception, _, i, _) => _logger.Warn(exception, $"Retry number:{i} for initializer {initializerName}")));
                
            var policyResult = await initPolicy.ExecuteAndCaptureAsync(init);
            if(policyResult.Outcome is OutcomeType.Successful)
                return;
                
            if (initTimeoutAttribute.Required)
            {
                var message = $"Required initializer {initializerName} failed with {policyResult.FinalException?.Message}, application will not start.";
                Console.WriteLine(message);
                _logger.Error(policyResult.FinalException, message);
                throw policyResult.FinalException!;
            }
            else
            {
                var message = $"Required initializer {initializerName} failed with {policyResult.FinalException?.Message}, continue in background.";
                Console.WriteLine(message);
                _logger.Warn(message); 
            }
        }
    }
}