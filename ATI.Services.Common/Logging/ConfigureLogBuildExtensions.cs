using ATI.Services.Common.Logging.Configuration;
using Microsoft.Extensions.Configuration;

namespace ATI.Services.Common.Logging;

public static class ConfigureLogBuildExtensions
{
    public static IConfigurationBuilder AddLogger(this IConfigurationBuilder builder)
    {
        var configurationRoot = builder.Build();
        var nLogOptions = configurationRoot.GetSection("NLogOptions").Get<NLogOptions>();
        var nLogConfigurator = new NLogConfigurator(nLogOptions);
        nLogConfigurator.ConfigureNLog();
        
        return builder;
    }
}