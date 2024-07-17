#nullable enable
using JetBrains.Annotations;

namespace ATI.Services.Common.Mattermost;

/// <summary>
/// Ответ на попытку создания сообщения
/// </summary>
[PublicAPI]
public class PostMessageResponse
{
    public required string Id { get; init; }
    public required string ChannelId { get; init; }
    public required string Message { get; init; }
}