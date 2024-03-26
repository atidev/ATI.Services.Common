using System.Collections.Generic;

namespace ATI.Services.Common.Variables
{
    public class ServiceVariablesOptions
    {
        public Dictionary<string, string> Variables { get; set; }
        public List<string> SupportedLocales { get; set; }
        
        public string GetServiceAsClientName()
        {
            if (Variables == null)
                return string.Empty;
            
            return Variables.TryGetValue("ServiceAsClientName", out var name) ? name : string.Empty;
        }
        
        public string GetServiceAsClientHeaderName()
        {
            if (Variables == null)
                return string.Empty;
            
            return Variables.TryGetValue("ServiceAsClientHeaderName", out var name) ? name : string.Empty;
        }
    }
}
