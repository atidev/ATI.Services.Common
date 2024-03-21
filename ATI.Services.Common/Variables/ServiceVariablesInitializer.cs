﻿using System;
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
            ServiceVariables.ServiceAsClientName = _options?.GetServiceAsClientName() ?? "";
            ServiceVariables.ServiceAsClientHeaderName = _options?.GetServiceAsClientHeaderName() ?? "";
            ServiceVariables.DefaultLocale = ServiceVariables.Variables.TryGetValue("DefaultLocale", out var locale) ? locale : "ru";
            var locales = _options?.SupportedLocales ?? new List<string> { ServiceVariables.DefaultLocale };
            ServiceVariables.SupportedLocales = new HashSet<string>(locales, StringComparer.OrdinalIgnoreCase);

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
