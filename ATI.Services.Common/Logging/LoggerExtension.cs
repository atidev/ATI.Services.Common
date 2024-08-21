using System;
using System.Collections.Generic;
using System.Text.Json;
using ATI.Services.Common.Serializers.SystemTextJsonSerialization;
using JetBrains.Annotations;
using NLog;

namespace ATI.Services.Common.Logging;

[PublicAPI]
public static class LoggerExtension
{
    public static void ErrorWithObject(this ILogger logger, Exception ex, string message, params object[] logObjects)
    {
        logger.LogWithObject(LogLevel.Error, ex, message, logObjects: logObjects);
    }

    public static void ErrorWithObject(this ILogger logger, Exception ex, params object[] logObjects)
    {
        logger.LogWithObject(LogLevel.Error, ex, logObjects: logObjects);
    }

    public static void ErrorWithObject(this ILogger logger, string message, params object[] logObjects)
    {
        logger.LogWithObject(LogLevel.Error, message: message, logObjects: logObjects);
    }
    
    [Obsolete("Use ErrorWithObject(this ILogger logger, string message, params object[] logObjects) instead")]
    public static void ErrorWithObject(this ILogger logger, params object[] logObjects)
    {
        logger.LogWithObject(LogLevel.Error, logObjects: logObjects);
    }

    public static void WarnWithObject(this ILogger logger, string message, params object[] logObjects)
    {
        logger.LogWithObject(LogLevel.Warn, null, message, null, logObjects);
    }
    
    public static void WarnWithObject(this ILogger logger, Exception ex, params object[] logObjects)
    {
        logger.LogWithObject(LogLevel.Warn, ex, logObjects: logObjects);
    }
    
    public static void LogWithObject(this ILogger logger,
        LogLevel logLevel,
        Exception ex = null,
        string message = null,
        Dictionary<object, object> additionalProperties = null,
        params object[] logObjects)
    {
        try
        {
            if (logger == null)
            {
                Console.WriteLine(
                    $"Логгер не инициализирован, stackTrace: {Environment.NewLine}{Environment.StackTrace}");
                return;
            }

            var json = string.Empty;
            if (logObjects != null && logObjects.Length != 0)
                json = JsonSerializer.Serialize(logObjects, SystemTextJsonCustomOptions.IgnoreUserSensitiveDataOptions);


            var eventInfo = new LogEventInfo(logLevel, logger.Name,
                message ?? ex?.Message ?? "No message provided. Exception is null.")
            {
                Properties = { new KeyValuePair<object, object>("logContext", json) }
            };

            if (additionalProperties != null)
                foreach (var additionalProperty in additionalProperties)
                {
                    eventInfo.Properties.TryAdd(additionalProperty.Key, additionalProperty.Value);
                }

            if (ex != null)
                eventInfo.Exception = ex;

            logger.Log(eventInfo);
        }
        catch (Exception e)
        {
            Console.WriteLine("Exception occured in Logger");
            Console.WriteLine(e);
        }
    }
}