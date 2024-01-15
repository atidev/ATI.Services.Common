using System;
using ATI.Services.Common.Logging.Configuration;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Layouts;
using ConfigurationManager = ATI.Services.Common.Behaviors.ConfigurationManager;

namespace ATI.Services.Common.Logging
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    public class LogHelper
    {
        public static void ConfigureNLogFromAppSettings()
        {
            var nLogOptions = ConfigurationManager.ConfigurationRoot.GetSection("NLogOptions").Get<NLogOptions>();
            var nLogConfigurator = new NLogConfigurator(nLogOptions);
            nLogConfigurator.ConfigureNLog();
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
    }
}