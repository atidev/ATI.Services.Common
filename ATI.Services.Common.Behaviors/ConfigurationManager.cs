using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;

namespace ATI.Services.Common.Behaviors;

public static class ConfigurationManager
{
    public static IConfigurationRoot ConfigurationRoot { get; set; } = null!;

    public static IConfigurationSection GetSection(string str) => ConfigurationRoot.GetSection(str);

    public static string AppSettings(string name) => ConfigurationRoot[$"AppSettings:{name}"]!;

    public static string LoggerSettings(string name) => ConfigurationRoot[$"LoggerSettings:{name}"]!;

    [PublicAPI]
    public static int GetApplicationPort()
    {
        var isEnv = int.TryParse(Environment.GetEnvironmentVariable("DEPLOY_PORT"), out var port);
        if (isEnv)
            return port;

        var isSettings = int.TryParse(AppSettings("Port"), out port);
        if (isSettings)
            return port;

        throw new Exception("Не удалось получить номер порта");
    }
}