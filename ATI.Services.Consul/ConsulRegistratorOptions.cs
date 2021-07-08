using System;
using System.Collections.Generic;

namespace ATI.Services.Consul
{
    public class ConsulRegistratorOptions
    {
        public string ProvideEnvironment { get; set; }
        public List<ConsulServiceOptions> ConsulServiceOptions { get; set; }
        public TimeSpan ReregistrationPeriod { get; set; } = TimeSpan.FromSeconds(30);
    }
}