#nullable enable
using JetBrains.Annotations;

namespace ATI.Services.Common.Mattermost;

/// <summary>
/// Модель отправки сообщения через webhook
/// </summary>
[PublicAPI]
public class MattermostWebHookRequestBody
{
    public required string Text { get; init; }
    public required string Username { get; init; }
    public required string IconEmoji { get; init; }
}