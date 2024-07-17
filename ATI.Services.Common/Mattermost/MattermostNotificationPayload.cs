using JetBrains.Annotations;
#nullable enable

namespace ATI.Services.Common.Mattermost;

[PublicAPI]
internal class MattermostNotificationPayload
{
    public required string ChannelId { get; init; }
    public string? RootId { get; init; }
    public required string Message { get; init; }
}