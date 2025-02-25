using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Logging;
using JetBrains.Annotations;
using NLog;
using Polly.Timeout;

namespace ATI.Services.Common.Http.Extensions;

public static class HttpResponseMessageExtensions
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    
    [PublicAPI]
    public static async Task<OperationResult<TResponse>> ParseHttpResponseAsync<TResponse>(
        this HttpResponseMessage responseMessage, 
        JsonSerializerOptions serializerOptions,
        CancellationToken cancellationToken = default)
    {
        if (!responseMessage.IsSuccessStatusCode) 
            return new OperationResult<TResponse>(OperationResult.GetActionStatusByHttpStatusCode(responseMessage.StatusCode));

        try
        {
            var result = await responseMessage.Content.ReadFromJsonAsync<TResponse>(serializerOptions, cancellationToken: cancellationToken);
            return new OperationResult<TResponse>(result, OperationResult.GetActionStatusByHttpStatusCode(responseMessage.StatusCode));
        }
        catch (Exception ex)
        {
            Logger.ErrorWithObject(ex, "Unsuccessfull response parsing", new
            {
                Method = responseMessage.RequestMessage.Method,
                Content = await responseMessage.Content.ReadAsStringAsync(cancellationToken),
                Headers = responseMessage.RequestMessage.Headers,
                FullUri = responseMessage.RequestMessage.RequestUri,
                TResponseClassName = typeof(TResponse).Name
            });
            return new OperationResult<TResponse>(ex);
        }
    }
    
    [PublicAPI]
    public static async Task<OperationResult<byte[]>> GetByteArrayFromHttpResponseAsync(
        this HttpResponseMessage responseMessage,
        CancellationToken cancellationToken = default)
    {
        if (!responseMessage.IsSuccessStatusCode) 
            return new OperationResult<byte[]>(OperationResult.GetActionStatusByHttpStatusCode(responseMessage.StatusCode));

        try
        {
            var result = await responseMessage.Content.ReadAsByteArrayAsync(cancellationToken);
            return new OperationResult<byte[]>(result, OperationResult.GetActionStatusByHttpStatusCode(responseMessage.StatusCode));
        }
        catch (Exception ex)
        {
            Logger.ErrorWithObject(ex, "Unsuccessfull response parsing", new
            {
                Method = responseMessage.RequestMessage.Method,
                Content = await responseMessage.Content.ReadAsStringAsync(cancellationToken),
                Headers = responseMessage.RequestMessage.Headers,
                FullUri = responseMessage.RequestMessage.RequestUri
            });
            return new OperationResult<byte[]>(ex);
        }
    }
    
    [PublicAPI]
    public static async Task<OperationResult<string>> GetStringFromHttpResponseAsync(
        this HttpResponseMessage responseMessage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await responseMessage.Content.ReadAsStringAsync(cancellationToken);
            return new OperationResult<string>(result, OperationResult.GetActionStatusByHttpStatusCode(responseMessage.StatusCode));
        }
        catch (Exception ex)
        {
            Logger.ErrorWithObject(ex, "Unsuccessfull response parsing", new
            {
                Method = responseMessage.RequestMessage.Method,
                Content = await responseMessage.Content.ReadAsStringAsync(cancellationToken),
                Headers = responseMessage.RequestMessage.Headers,
                FullUri = responseMessage.RequestMessage.RequestUri
            });
            return new OperationResult<string>(ex);
        }
    }
}