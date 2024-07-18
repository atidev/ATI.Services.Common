using System.Collections.Generic;
using System.Net.Http;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Policies;
using System.Threading.Tasks;
using NLog;
using JetBrains.Annotations;
using Newtonsoft.Json;
using ATI.Services.Common.Logging;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using ATI.Services.Common.Extensions;
using System.Text;

namespace ATI.Services.Common.Http.Extensions.Newtonsoft;

public static class HttpClientExtensionsForNewtonsoft
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    private static readonly JsonSerializer SnakeCaseSerializer = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy {
                ProcessDictionaryKeys = true,
                OverrideSpecifiedNames = true
            }
        }
    };

    #region Базовые методы

    [PublicAPI]
    public static async Task<OperationResult<TResponse>> SendAsync<TResponse>(
        this HttpClient httpClient,
        HttpMethod httpMethod,
        string url,
        string metricEntity,
        string urlTemplate = null,
        Dictionary<string, string> headers = null,
        JsonSerializer serializer = null,
        RetryPolicySettings retryPolicySettings = null,
        ILogger logger = null,
        bool checkResponseStatusCode = true
    )
    {
        try
        {
            logger ??= Logger;
            serializer ??= SnakeCaseSerializer;

            using var requestMessage 
                = HttpClientExtensions.CreateHttpRequestMessageAndSetBaseFields(httpMethod, url, metricEntity, urlTemplate, headers, retryPolicySettings);

            using var responseMessage = await httpClient.SendAsync(requestMessage);

            if (checkResponseStatusCode && !responseMessage.IsSuccessStatusCode)
                return new OperationResult<TResponse>(responseMessage.StatusCode);

            using var stream = await responseMessage.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            var response = serializer.Deserialize<TResponse>(jsonReader);

            return new OperationResult<TResponse>(response, OperationResult.GetActionStatusByHttpStatusCode(responseMessage.StatusCode));
        }
        catch (Exception ex)
        {
            logger.ErrorWithObject(ex, new { httpMethod, url, headers });

            return new OperationResult<TResponse>(ex);
        }
    }
    
    [PublicAPI]
    public static async Task<OperationResult<TResponse>> SendAsync<TRequest, TResponse>(
        this HttpClient httpClient,
        HttpMethod httpMethod,
        string url,
        TRequest request,
        string metricEntity,
        string urlTemplate = null,
        Dictionary<string, string> headers = null,
        JsonSerializer serializer = null,
        RetryPolicySettings retryPolicySettings = null,
        ILogger logger = null,
        bool checkResponseStatusCode = true
    )
    {
        try
        {
            logger ??= Logger;
            serializer ??= SnakeCaseSerializer;

            using var requestMessage =
                HttpClientExtensions.CreateHttpRequestMessageAndSetBaseFields(httpMethod, url, metricEntity, urlTemplate, headers, retryPolicySettings);

            var content = request != null ? serializer.Serialize(request) : string.Empty; 
            requestMessage.Content = new StringContent(content, Encoding.UTF8, "application/json");

            using var responseMessage = await httpClient.SendAsync(requestMessage);

            if (checkResponseStatusCode && !responseMessage.IsSuccessStatusCode)
                return new OperationResult<TResponse>(responseMessage.StatusCode);

            using var stream = await responseMessage.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(reader);
            var response = serializer.Deserialize<TResponse>(jsonReader);

            return new OperationResult<TResponse>(response, OperationResult.GetActionStatusByHttpStatusCode(responseMessage.StatusCode));
        }
        catch (Exception ex)
        {
            logger.ErrorWithObject(ex, new { httpMethod, url, request, headers });

            return new OperationResult<TResponse>(ex);
        }
    }

    [PublicAPI]
    public static async Task<OperationResult<string>> SendAsync<TRequest>(
        this HttpClient httpClient,
        HttpMethod httpMethod,
        string url,
        TRequest request,
        string metricEntity,
        string urlTemplate = null,
        Dictionary<string, string> headers = null,
        JsonSerializer serializer = null,
        RetryPolicySettings retryPolicySettings = null,
        ILogger logger = null
    )
    {
        try
        {
            logger ??= Logger;
            serializer ??= SnakeCaseSerializer;

            using var requestMessage =
                HttpClientExtensions.CreateHttpRequestMessageAndSetBaseFields(httpMethod, url, metricEntity, urlTemplate, headers, retryPolicySettings);

            var content = request != null ? serializer.Serialize(request) : string.Empty;
            requestMessage.Content = new StringContent(content, Encoding.UTF8, "application/json");

            using var responseMessage = await httpClient.SendAsync(requestMessage);

            return await responseMessage.GetStringFromHttpResponseAsync();
        }
        catch (Exception ex)
        {
            logger.ErrorWithObject(ex, new { httpMethod, url, request, headers });

            return new OperationResult<string>(ex);
        }
    }

    #endregion

    #region Производные методы

    [PublicAPI]
    public static Task<OperationResult<TResponse>> GetAsync<TResponse>(
        this HttpClient httpClient,
        string url,
        string metricEntity,
        string urlTemplate = null,
        Dictionary<string, string> headers = null,
        JsonSerializer serializer = null,
        RetryPolicySettings retryPolicySettings = null
    )
      => SendAsync<TResponse>(
            httpClient,
            HttpMethod.Get,
            url,
            metricEntity,
            urlTemplate,
            headers,
            serializer,
            retryPolicySettings
         );

    [PublicAPI]
    public static Task<OperationResult<string>> PostAsync<TRequest>(
        this HttpClient httpClient,
        string url,
        TRequest request,
        string metricEntity,
        string urlTemplate = null,
        Dictionary<string, string> headers = null,
        JsonSerializer serializer = null,
        RetryPolicySettings retryPolicySettings = null
    )
      => SendAsync(
            httpClient,
            HttpMethod.Post,
            url,
            request,
            metricEntity,
            urlTemplate,
            headers,
            serializer,
            retryPolicySettings
         );

    [PublicAPI]
    public static Task<OperationResult<TResponse>> PostAsync<TRequest, TResponse>
    (
        this HttpClient httpClient,
        string url,
        TRequest request,
        string metricEntity,
        string urlTemplate = null,
        Dictionary<string, string> headers = null,
        JsonSerializer serializer = null,
        RetryPolicySettings retryPolicySettings = null
    )
      => SendAsync<TRequest, TResponse>(
            httpClient,
            HttpMethod.Post,
            url,
            request,
            metricEntity,
            urlTemplate,
            headers,
            serializer,
            retryPolicySettings
         );
    
    [PublicAPI]
    public static Task<OperationResult<TResponse>> DeleteAsync<TRequest, TResponse>
    (
        this HttpClient httpClient,
        string url,
        TRequest request,
        string metricEntity,
        string urlTemplate = null,
        Dictionary<string, string> headers = null,
        JsonSerializer serializer = null,
        RetryPolicySettings retryPolicySettings = null
    )
      => SendAsync<TRequest, TResponse>(
            httpClient,
            HttpMethod.Delete,
            url,
            request,
            metricEntity,
            urlTemplate,
            headers,
            serializer,
            retryPolicySettings
         );

    [PublicAPI]
    public static Task<OperationResult<string>> DeleteAsync
    (
        this HttpClient httpClient,
        string url,
        string metricEntity,
        string urlTemplate = null,
        Dictionary<string, string> headers = null,
        RetryPolicySettings retryPolicySettings = null
    )
      => HttpClientExtensions.GetStringAsync(
            httpClient,
            HttpMethod.Delete,
            url,
            metricEntity,
            urlTemplate,
            headers,
            retryPolicySettings
         );

    [PublicAPI]
    public static Task<OperationResult<TResponse>> DeleteAsync<TResponse>
    (
        this HttpClient httpClient,
        string url,
        string metricEntity,
        string urlTemplate = null,
        Dictionary<string, string> headers = null,
        JsonSerializer serializer = null,
        RetryPolicySettings retryPolicySettings = null
    )
      => SendAsync<TResponse>(
            httpClient,
            HttpMethod.Delete,
            url,
            metricEntity,
            urlTemplate,
            headers,
            serializer,
            retryPolicySettings
         );

    [PublicAPI]
    public static Task<OperationResult<string>> PutAsync<TRequest>(
        this HttpClient httpClient,
        string url,
        TRequest request,
        string metricEntity,
        string urlTemplate = null,
        Dictionary<string, string> headers = null,
        JsonSerializer serializer = null,
        RetryPolicySettings retryPolicySettings = null
    )
      => SendAsync(
            httpClient,
            HttpMethod.Put,
            url,
            request,
            metricEntity,
            urlTemplate,
            headers,
            serializer,
            retryPolicySettings
         );

    [PublicAPI]
    public static Task<OperationResult<TResponse>> PutAsync<TRequest, TResponse>
    (
        this HttpClient httpClient,
        string url,
        TRequest request,
        string metricEntity,
        string urlTemplate = null,
        Dictionary<string, string> headers = null,
        JsonSerializer serializer = null,
        RetryPolicySettings retryPolicySettings = null
    )
      => SendAsync<TRequest, TResponse>(
            httpClient,
            HttpMethod.Put,
            url,
            request,
            metricEntity,
            urlTemplate,
            headers,
            serializer,
            retryPolicySettings
         );

    #endregion
}
