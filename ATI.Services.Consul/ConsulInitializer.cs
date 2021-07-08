using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Initializers;
using ATI.Services.Common.Initializers.Interfaces;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace ATI.Services.Consul
{
    [PublicAPI]
    [InitializeOrder(Order = InitializeOrder.Sixth)]
    public class ConsulInitializer : IInitializer
    {
        private static bool _initialized;
        private readonly ConsulRegistratorOptions _consulRegistratorOptions;

        public ConsulInitializer(IOptions<ConsulRegistratorOptions> consulRegistratorOptions)
        {
            _consulRegistratorOptions = consulRegistratorOptions.Value;
        }

        public async Task InitializeAsync()
        {
            if (_initialized)
                return;

            string consulEnabledString;
            if ((consulEnabledString = ConfigurationManager.AppSettings("ConsulEnabled")) == null 
                || bool.TryParse(consulEnabledString, out var consulEnabled) && consulEnabled)
            {
                await ConsulRegistrator.RegisterServicesAsync(_consulRegistratorOptions, ConfigurationManager.GetApplicationPort());
            }

            _initialized = true;
        }
    }
}