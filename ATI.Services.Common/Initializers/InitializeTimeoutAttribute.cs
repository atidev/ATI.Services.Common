using System;

namespace ATI.Services.Common.Initializers;

/// <summary>
/// Attribute for setting initialization timeout and behavior in case of timeout
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class InitializeTimeoutAttribute : Attribute
{
    /// <summary>
    /// Initialization timeout in seconds
    /// When set, enables timeout for initialization,
    /// in case of Timeout and Required = true, application will not start
    /// in case of Timeout and Required = false, application will start without waiting for initialization if this service
    /// Default value is 10 seconds 
    /// </summary>
    public int InitTimeoutSec { get; set; } = 10;
    
    /// <summary>
    /// Is initialization required
    /// When true, application will not start if initialization failed due to timeout or exception
    /// </summary>
    public bool Required { get; set; } = false;
}