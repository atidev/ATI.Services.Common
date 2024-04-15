using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Policies;
using JetBrains.Annotations;
using NLog;

namespace ATI.Services.Common.Http.Extensions;

public static class HttpClientExtensions
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    private static readonly JsonSerializerOptions SnakeCaseSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseUpper,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };


    public static void SetBaseFields(
        this HttpClient httpClient,
        string serviceAsClientName,
        string serviceAsClientHeaderName,
        Dictionary<string, string> additionalHeaders)
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

    [PublicAPI]
    public static async Task<OperationResult<TResponse>> SendAsync<TResponse>(this HttpClient httpClient,
        HttpMethod httpMethod,
        string url,
        string metricEntity,
        string urlTemplate = null,
        Dictionary<string, string> headers = null,
        JsonSerializerOptions serializerOptions = null,
        RetryPolicySettings retryPolicySettings = null)
    {
        try
        {
            using var requestMessage = CreateHttpRequestMessageAndSetBaseFields(httpMethod, url, metricEntity, urlTemplate, headers, retryPolicySettings);

            using var responseMessage = await httpClient.SendAsync(requestMessage);
            
            return await responseMessage.ParseHttpResponseAsync<TResponse>(serializerOptions ?? SnakeCaseSerializerOptions);
        }
        catch (Exception ex)
        {
            Logger.ErrorWithObject(ex, new { httpMethod, url, headers });
            return new OperationResult<TResponse>(ex);
        }
    }

    [PublicAPI]
    public static async Task<OperationResult<TResponse>> SendAsync<TRequest, TResponse>(this HttpClient httpClient,
        HttpMethod httpMethod,
        string url,
        TRequest request,
        string metricEntity,
        string urlTemplate = null,
        Dictionary<string, string> headers = null,
        JsonSerializerOptions serializerOptions = null,
        RetryPolicySettings retryPolicySettings = null)
    {
        try
        {
            using var requestMessage = CreateHttpRequestMessageAndSetBaseFields(httpMethod, url, metricEntity, urlTemplate, headers, retryPolicySettings);

            var serializeOptions = serializerOptions ?? SnakeCaseSerializerOptions;
            requestMessage.SetContent(request, serializeOptions);

            using var responseMessage = await httpClient.SendAsync(requestMessage);
            return await responseMessage.ParseHttpResponseAsync<TResponse>(serializeOptions);
        }
        catch (Exception ex)
        {
            Logger.ErrorWithObject(ex, new { httpMethod, url, request, headers });
            return new OperationResult<TResponse>(ex);
        }
    }

    [PublicAPI]
    public static async Task<OperationResult<TResponse>> SendAsync<TResponse>(this HttpClient httpClient,
        HttpMethod httpMethod,
        string url,
        HttpContent content,
        string metricEntity,
        string urlTemplate = null,
        Dictionary<string, string> headers = null,
        JsonSerializerOptions serializerOptions = null,
        RetryPolicySettings retryPolicySettings = null)
    {
        try
        {
            using var requestMessage = CreateHttpRequestMessageAndSetBaseFields(httpMethod, url, metricEntity, urlTemplate, headers, retryPolicySettings);
            requestMessage.Content = content;

            using var responseMessage = await httpClient.SendAsync(requestMessage);
            return await responseMessage.ParseHttpResponseAsync<TResponse>(serializerOptions ?? SnakeCaseSerializerOptions);
        }
        catch (Exception ex)
        {
            Logger.ErrorWithObject(ex, new { httpMethod, url, headers });
            return new OperationResult<TResponse>(ex);
        }
    }

    [PublicAPI]
    public static async Task<OperationResult<byte[]>> GetByteArrayAsync(this HttpClient httpClient,
        HttpMethod httpMethod,
        string url,
        HttpContent content,
        string metricEntity,
        string urlTemplate = null,
        Dictionary<string, string> headers = null,
        RetryPolicySettings retryPolicySettings = null)
    {
        try
        {
            using var requestMessage = CreateHttpRequestMessageAndSetBaseFields(httpMethod, url, metricEntity, urlTemplate, headers, retryPolicySettings);
            requestMessage.Content = content;

            using var responseMessage = await httpClient.SendAsync(requestMessage);
            return await responseMessage.GetByteArrayFromHttpResponseAsync();
        }
        catch (Exception ex)
        {
            Logger.ErrorWithObject(ex, new { httpMethod, url, headers });
            return new OperationResult<byte[]>(ex);
        }
    }

    [PublicAPI]
    public static HttpRequestMessage CreateHttpRequestMessageAndSetBaseFields(
        HttpMethod httpMethod,
        string url,
        string metricEntity,
        string urlTemplate,
        Dictionary<string, string> headers,
        RetryPolicySettings retryPolicySettings)
    {
        var requestMessage = new HttpRequestMessage(httpMethod, url);

        requestMessage.Options.AddMetricEntity(metricEntity);
        if (urlTemplate != null)
            requestMessage.Options.AddUrlTemplate(urlTemplate);

        if (headers != null)
        {
            foreach (var header in headers)
                requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (retryPolicySettings != null)
            requestMessage.Options.AddRetryPolicy(retryPolicySettings);

        return requestMessage;
    }
}