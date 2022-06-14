using System.Collections.Generic;

namespace ATI.Services.Common.Variables
{
    public class ServiceVariablesOptions
    {
        public Dictionary<string, string> Variables { get; set; }
        public string[] SupportedLocales { get; set; }
    }
}
