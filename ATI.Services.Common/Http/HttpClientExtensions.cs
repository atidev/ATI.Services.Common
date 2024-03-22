using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using ATI.Services.Common.Variables;
using Microsoft.Extensions.Configuration;
using ConfigurationManager = ATI.Services.Common.Behaviors.ConfigurationManager;

namespace ATI.Services.Common.Http;

internal static class HttpClientExtensions
{
    public static void SetBaseFields(this HttpClient httpClient, Dictionary<string,string> additionalHeaders)
    {
        var serviceVariablesOptions = ConfigurationManager.GetSection(nameof(ServiceVariablesOptions)).Get<ServiceVariablesOptions>();
        
        var serviceAsClientName = ServiceVariables.ServiceAsClientName;
        var serviceAsClientHeaderName = ServiceVariables.ServiceAsClientHeaderName;
        
        if(string.IsNullOrEmpty(serviceAsClientName))
            serviceAsClientName =  serviceVariablesOptions.GetServiceAsClientName();
        if(string.IsNullOrEmpty(serviceAsClientHeaderName))
            serviceAsClientHeaderName =  serviceVariablesOptions.GetServiceAsClientHeaderName();
        
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(serviceAsClientName) &&
            !string.IsNullOrEmpty(serviceAsClientHeaderName))
        {
            httpClient.DefaultRequestHeaders.Add(
                ServiceVariables.ServiceAsClientHeaderName,
                ServiceVariables.ServiceAsClientName);
        }

        if (additionalHeaders is { Count: > 0 })
        {
            foreach (var header in additionalHeaders)
            {
                httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }
        }
    }
}