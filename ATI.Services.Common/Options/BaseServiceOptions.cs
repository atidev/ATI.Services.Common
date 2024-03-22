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
    /// Timeout for one request. If you use RetryPolicy - it will be also a timeout for one request (not total time of policy)
    /// </summary>
    public TimeSpan TimeOut { get; set; }
    
    public int RetryCount { get; set; } = 3;
    
    public TimeSpan MedianFirstRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Number of exceptions after which CB will be opened (will stop making requests)
    /// </summary>
    public int CircuitBreakerExceptionsCount { get; set; } = 20;
    
    /// <summary>
    /// Time after which CB will be closed (will make requests)
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
    
    public List<string> HttpMethodsToRetry { get; set; }
}