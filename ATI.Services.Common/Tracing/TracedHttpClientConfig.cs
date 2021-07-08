using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ATI.Services.Common.Tracing
{
    public class TracedHttpClientConfig
    {
        public JsonSerializer Serializer { get; set; } = new()
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy
                {
                    ProcessDictionaryKeys = true
                }
            }
        };
        public string ServiceName { get; set; }
        public TimeSpan Timeout { get; set; }
        public Dictionary<string, string> Headers { get; [PublicAPI] set; } = new();
    }
}