using System;

namespace ATI.Services.Common.Initializers;

[AttributeUsage(AttributeTargets.Class)]
public class InitializeTimeoutAttribute : Attribute
{
    /// <summary>
    /// Initialization timeout in seconds 
    /// </summary>
    public int InitTimeoutSec { get; set; } = 10;
    /// <summary>
    /// Is initialization required
    /// 
    /// </summary>
    public bool Required { get; set; } = false;
}