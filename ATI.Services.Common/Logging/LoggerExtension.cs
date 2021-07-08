using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NLog;

namespace ATI.Services.Common.Logging
{
    public static class LoggerExtension
    {
        private const string NullExceptionMessage = "Exception is null. No message provided.";
        public static void ErrorWithObject(this ILogger logger, Exception ex, string message, params object[] logObjects)
        {
            try
            {
                if (logger == null)
                {
                    Console.WriteLine($"Логгер не инициализирован, stackTrace: {Environment.NewLine}{Environment.StackTrace}");
                    return;
                }
                
                var json = "";
                if (logObjects != null && logObjects.Length != 0)
                    json = JsonConvert.SerializeObject(logObjects);
                
                var eventInfo = new LogEventInfo(LogLevel.Error, logger.Name, message ?? ex?.Message ?? NullExceptionMessage)
                {
                    Exception = ex,
                    Properties = { new KeyValuePair<object, object>("logContext", json) }
                };
                logger.Log(eventInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception occured in Logger");
                Console.WriteLine(e);
            }
        }

        public static void ErrorWithObject(this ILogger logger, Exception ex, params object[] logObjects)
        {
            try
            {   
                if (logger == null)
                {
                    Console.WriteLine($"Логгер не инициализирован, stackTrace: {Environment.NewLine}{Environment.StackTrace}");
                    return;
                }
                
                var json = "";
                if (logObjects != null && logObjects.Length != 0)
                    json = JsonConvert.SerializeObject(logObjects);
                
                var eventInfo = new LogEventInfo(LogLevel.Error, logger.Name, ex?.Message ?? NullExceptionMessage)
                {
                    Exception = ex,
                    Properties = { new KeyValuePair<object, object>("logContext", json) }
                };
                logger.Log(eventInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception occured in Logger");
                Console.WriteLine(e);
            }
        }

        public static void WarnWithObject(this ILogger logger, string message, params object[] logObjects)
        {
            try
            {
                if (logger == null)
                {
                    Console.WriteLine($"Логгер не инициализирован, stackTrace: {Environment.NewLine}{Environment.StackTrace}");
                    return;
                }
                
                var json = "";
                if (logObjects != null && logObjects.Length != 0)
                    json = JsonConvert.SerializeObject(logObjects);
                
                var eventInfo = new LogEventInfo(LogLevel.Warn, logger.Name, message)
                {
                    Properties = { new KeyValuePair<object, object>("logContext", json) }
                };
                logger.Log(eventInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception occured in Logger");
                Console.WriteLine(e);
            }
        }

        internal static void LogLongRequest(this ILogger logger, LogSource logSource, object metricContext)
        {
            try
            {
                if (logger == null)
                {
                    Console.WriteLine($"Логгер не инициализирован, stackTrace: {Environment.NewLine}{Environment.StackTrace}");
                    return;
                }
                
                if (metricContext == null)
                    return;

                var json = JsonConvert.SerializeObject(metricContext);

                var eventInfo = new LogEventInfo(LogLevel.Warn, logger.Name, "Long request WARN.")
                {
                    Properties =
                    {
                        new KeyValuePair<object, object>("metricSource", logSource.ToString()),
                        new KeyValuePair<object, object>("metricString", json),
                    }
                };
                logger.Log(eventInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception occured in Logger");
                Console.WriteLine(e);
            }
        }
    }
}