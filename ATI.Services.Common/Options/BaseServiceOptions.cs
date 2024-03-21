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
    
    /// <summary>
    /// Таймаут одного запроса к сервису. Если настроена RetryPolicy - таймаут каждого запроса к сервису (не суммарный)
    /// </summary>
    public TimeSpan TimeOut { get; set; }

    /// <summary>
    /// Количество повторных запросов
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Количество ошибок, после которых CB откроется (перестанет принимать запросы)
    /// </summary>
    public int CircuitBreakerExceptionsCount { get; set; } = 20;
    
    /// <summary>
    /// Количество времени, по истечению которых CB закроется (начнет принимать запросы)
    /// </summary>
    public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromSeconds(2);
    
    public string Environment { get; set; }
    public TimeSpan? LongRequestTime { get; set; }

    public Dictionary<string, string> AdditionalHeaders { get; set; }

    public bool AddCultureToRequest { get; set; } = true;
    public List<string> HeadersToProxy { get; set; } = new();
    public SerializerType SerializerType { get; set; } = SerializerType.Newtonsoft;
    public virtual Func<LogLevel, LogLevel> LogLevelOverride => level => level;

    public bool UseHttpClientFactory { get; set; }
}