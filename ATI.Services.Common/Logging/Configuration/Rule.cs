using NLog;

namespace ATI.Services.Common.Logging.Configuration;

public class Rule
{
    public string TargetName { get; set; }
    public LogLevel MinLevel { get; set; } = LogLevel.Warn;
    public LogLevel MaxLevel { get; set; } = LogLevel.Off;
    public string LoggerNamePattern { get; set; } = "*";
}