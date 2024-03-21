using System;

namespace ATI.Services.Common.Variables;

internal static class StaticServiceProvider
{
    private static IServiceProvider? _serviceProvider;

    /// <summary>
    /// Provides static access to the framework's services provider
    /// </summary>
    public static IServiceProvider? ServiceProvider
    {
        get => _serviceProvider;
        set
        {
            // If _services already initialized -> return
            if (_serviceProvider is not null)
                return;

            _serviceProvider = value;
        }
    }
}