using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ATI.Services.Common.Http;

internal static class HttpClientExtensions
{
    public static void SetBaseFields(
        this HttpClient httpClient, 
        string serviceAsClientName,
        string serviceAsClientHeaderName,
        Dictionary<string,string> additionalHeaders)
    {
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrEmpty(serviceAsClientName) &&
            !string.IsNullOrEmpty(serviceAsClientHeaderName))
        {
            httpClient.DefaultRequestHeaders.Add(
                serviceAsClientHeaderName,
                serviceAsClientName);
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