using System.Net.Http;
using System.Text;
using System.Text.Json;
using JetBrains.Annotations;

namespace ATI.Services.Common.Http.Extensions;

public static class HttpRequestMessageExtensions
{
    [PublicAPI]
    public static void SetContent<TRequest>(this HttpRequestMessage requestMessage, TRequest request, JsonSerializerOptions serializerOptions)
    {
        var content = request != null ? JsonSerializer.Serialize(request, serializerOptions) : string.Empty;
        requestMessage.Content = new StringContent(content, Encoding.UTF8, "application/json");
    }
}