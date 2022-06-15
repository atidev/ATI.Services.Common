using System.Collections.Generic;
using System.Threading.Tasks;
using ATI.Services.Common.Initializers;
using ATI.Services.Common.Initializers.Interfaces;
using Microsoft.Extensions.Options;

namespace ATI.Services.Common.Variables
{
    [InitializeOrder(Order = InitializeOrder.First)]
    public class ServiceVariablesInitializer : IInitializer
    {
        private static bool _initialized;
        private readonly ServiceVariablesOptions _options;

        public ServiceVariablesInitializer(IOptions<ServiceVariablesOptions> options)
        {
            _options = options.Value;
        }

        public Task InitializeAsync()
        {
            if (_initialized)
            {
                return Task.CompletedTask;
            }

            ServiceVariables.Variables = _options?.Variables ?? new Dictionary<string, string>();
            ServiceVariables.ServiceAsClientName = ServiceVariables.Variables.TryGetValue("ServiceAsClientName", out var name) ? name : "";
            ServiceVariables.ServiceAsClientHeaderName = ServiceVariables.Variables.TryGetValue("ServiceAsClientHeaderName", out var headerName) ? headerName : "";
            ServiceVariables.DefaultLocale = ServiceVariables.Variables.TryGetValue("DefaultLocale", out var locale) ? locale : "ru";
            ServiceVariables.SupportedLocales = _options?.SupportedLocales ?? new[] { ServiceVariables.DefaultLocale };

            _initialized = true;
            return Task.CompletedTask;
        }
        
        public string InitStartConsoleMessage()
        {
            return "Start Service Variables initializer";
        }

        public string InitEndConsoleMessage()
        {
            return $"End Service Variables initializer, result {_initialized}";
        }
    }
}
