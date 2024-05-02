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

    private string _serviceName;
    /// <summary>
    /// Name for HttpClientFactory and logs
    /// </summary>
    public string ServiceName
    {
        get => _serviceName ?? ConsulName;
        set => _serviceName = value;
    }

    /// <summary>
    /// Timeout for one request. If you use RetryPolicy - it will be also a timeout for one request (not total time of policy)
    /// </summary>
    public TimeSpan TimeOut { get; set; }
    
    /// <summary>
    /// Set 0 if you dont want to use RetryPolicy
    /// </summary>
    public int RetryCount { get; set; } = 3;
    
    /// <summary>
    /// Delay between retries
    /// Median for spreading queries over time
    /// </summary>
    public TimeSpan MedianFirstRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Number of exceptions after which CB will be opened (will stop making requests)
    /// Set 0 if you dont want to use CB
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
    
    /// <summary>
    /// Http methods to retry
    /// If not set - retry only GET methods
    /// </summary>
    public List<string> HttpMethodsToRetry { get; set; }

    /// <summary>
    /// Lifetime of the connection
    /// Must be not infinite if you use HttpClient in static Typed Client 
    /// https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines
    /// </summary>
    public TimeSpan PooledConnectionLifetime { get; set; } = TimeSpan.FromMinutes(15);
}