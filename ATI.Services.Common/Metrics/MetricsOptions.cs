using System;
using System.Collections.Generic;

namespace ATI.Services.Common.Metrics;

public class MetricsOptions
{
    /// <summary>
    /// User's additional labels and headers
    /// Keys are labels' names
    /// Values are headers' names
    /// </summary>
    public Dictionary<string, string> LabelsAndHeaders { get; set; }
        
    public TimeSpan? DefaultLongRequestTime { get; set; }
}