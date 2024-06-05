using System;

namespace ATI.Services.Common.Policies;

public class RetryPolicySettings
{
    /// <summary>
    /// Timeout for one request. If you use RetryPolicy - it will be also a timeout for one request (not total time of policy)
    /// </summary>
    public TimeSpan? TimeOut { get; set; }
    
    public int? RetryCount { get; set; }
    
    /// <summary>
    /// Delay between retries
    /// Median for spreading queries over time
    /// </summary>
    public TimeSpan? MedianFirstRetryDelay { get; set; }

    /// <summary>
    /// Number of exceptions after which CB will be opened (will stop making requests)
    /// </summary>
    public int? CircuitBreakerExceptionsCount { get; set; }
    
    /// <summary>
    /// Time after which CB will be closed (will make requests)
    /// </summary>
    public TimeSpan? CircuitBreakerDuration { get; set; }
}