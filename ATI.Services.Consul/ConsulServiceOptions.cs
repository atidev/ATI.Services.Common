using System.Collections.Generic;
using Consul;

namespace ATI.Services.Consul
{
    public class ConsulServiceOptions
    {
        public string ServiceName { get; set; }
        public string[] Tags { get; set; }
        public AgentServiceCheck Check { get; set; }
        public Dictionary<string, string> SwaggerUrls { get; set; } = new();
    }
}