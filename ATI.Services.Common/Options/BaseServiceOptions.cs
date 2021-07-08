using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace ATI.Services.Common.Options
{
    [PublicAPI]
    public class BaseServiceOptions
    {
        public string ConsulName { get; set; }
        public TimeSpan TimeOut { get; set; }
        public string Environment { get; set; }
        public TimeSpan? LongRequestTime { get; set; }
        
        public Dictionary<string, string> AdditionalHeaders { get; set; }
    }
}