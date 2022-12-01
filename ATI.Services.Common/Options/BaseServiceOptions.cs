using System;
using System.Collections.Generic;
using ATI.Services.Common.Serializers;
using JetBrains.Annotations;
using NLog;

namespace ATI.Services.Common.Options;

[PublicAPI]
public class BaseServiceOptions
{
    public string ConsulName { get; set; }
    public TimeSpan TimeOut { get; set; }
    public string Environment { get; set; }
    public TimeSpan? LongRequestTime { get; set; }

    public Dictionary<string, string> AdditionalHeaders { get; set; }

    public bool AddCultureToRequest { get; set; } = true;
    public List<string> HeadersToProxy { get; set; } = new();
    public SerializerType SerializerType { get; set; } = SerializerType.Newtonsoft;
    public virtual Func<LogLevel, LogLevel> LogLevelOverride => level => level;
}