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
}