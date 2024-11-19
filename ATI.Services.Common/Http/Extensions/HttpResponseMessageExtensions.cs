using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Logging;
using JetBrains.Annotations;
using NLog;

namespace ATI.Services.Common.Http.Extensions;

public static class HttpResponseMessageExtensions
{
    private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();
    
    [PublicAPI]
    public static async Task<OperationResult<TResponse>> ParseHttpResponseAsync<TResponse>(this HttpResponseMessage responseMessage, JsonSerializerOptions serializerOptions)
    {
        if (!responseMessage.IsSuccessStatusCode) 
            return responseMessage.StatusCode.ToOperationResult<TResponse>();

        try
        {
            var result = await responseMessage.Content.ReadFromJsonAsync<TResponse>(serializerOptions);
            return responseMessage.StatusCode.ToOperationResult(result);
        }
        catch (Exception ex)
        {
            Logger.ErrorWithObject(ex, "Unsuccessfull response parsing", new
            {
                responseMessage.RequestMessage?.Method,
                Content = await responseMessage.Content.ReadAsStringAsync(),
                responseMessage.RequestMessage?.Headers,
                FullUri = responseMessage.RequestMessage?.RequestUri,
                TResponseClassName = typeof(TResponse).Name
            });
            return new OperationResult<TResponse>(ex);
        }
    }
    
    [PublicAPI]
    public static async Task<OperationResult<byte[]>> GetByteArrayFromHttpResponseAsync(this HttpResponseMessage responseMessage)
    {
        if (!responseMessage.IsSuccessStatusCode) 
            return responseMessage.StatusCode.ToOperationResult<byte[]>();

        try
        {
            var result = await responseMessage.Content.ReadAsByteArrayAsync();
            return responseMessage.StatusCode.ToOperationResult(result);
        }
        catch (Exception ex)
        {
            Logger.ErrorWithObject(ex, "Unsuccessfull response parsing", new
            {
                responseMessage.RequestMessage?.Method,
                Content = await responseMessage.Content.ReadAsStringAsync(),
                responseMessage.RequestMessage?.Headers,
                FullUri = responseMessage.RequestMessage?.RequestUri
            });
            return new OperationResult<byte[]>(ex);
        }
    }
    
    [PublicAPI]
    public static async Task<OperationResult<string>> GetStringFromHttpResponseAsync(this HttpResponseMessage responseMessage)
    {
        try
        {
            var result = await responseMessage.Content.ReadAsStringAsync();
            return responseMessage.StatusCode.ToOperationResult(result);
        }
        catch (Exception ex)
        {
            Logger.ErrorWithObject(ex, "Unsuccessfull response parsing", new
            {
                responseMessage.RequestMessage?.Method,
                Content = await responseMessage.Content.ReadAsStringAsync(),
                responseMessage.RequestMessage?.Headers,
                FullUri = responseMessage.RequestMessage?.RequestUri
            });
            return new OperationResult<string>(ex);
        }
    }
}