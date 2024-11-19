using System;
using ATI.Services.Common.Logging.Configuration;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Layouts;
using NLog.Web;
using ConfigurationManager = ATI.Services.Common.Behaviors.ConfigurationManager;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace ATI.Services.Common.Logging
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class LogHelper
    {
        public static LogFactory ConfigureNLogFromAppSettings()
        {
            var nLogOptions = ConfigurationManager.ConfigurationRoot.GetSection("NLogOptions").Get<NLogOptions>();
            var nLogConfigurator = new NLogConfigurator(nLogOptions);
            return nLogConfigurator.ConfigureNLog();
        }
        
        public static void ConfigureNlog(IWebHostEnvironment env)
        {
            try
            {
                var configPath = $"nlog.{env.EnvironmentName}.config";
                LogManager.Setup().LoadConfigurationFromFile(configPath);
            }
            catch (Exception exception)
            {
                LogManager.Setup().LoadConfigurationFromFile("nlog.config");
                LogManager.GetCurrentClassLogger().Error(exception);
            }
        }
        
        public static void ConfigureMetricsLoggers()
        {
            var loggingConfiguration = LogManager.Configuration;

            loggingConfiguration.Variables.Add("JsonLayout", new SimpleLayout(ConfigurationManager.LoggerSettings("JsonLayout")));

            LogManager.Setup().LoadConfiguration(loggingConfiguration);
        }

        public static ILogger GetMicrosoftLogger(string categoryName)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                var configuration = LogManager.Configuration;
                builder.AddNLog(configuration);
            });
            
            return loggerFactory.CreateLogger(categoryName);
        }
    }
}