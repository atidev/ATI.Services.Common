#nullable enable
using JetBrains.Annotations;

namespace ATI.Services.Common.Mattermost;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
internal class MattermostNotificationPayload
{
    /// <summary>
    /// Id канала
    /// </summary>
    public required string ChannelId { get; init; }

    /// <summary>
    /// Текст сообщения
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Id корневого сообщения (для отправки в тред)
    /// </summary>
    public string? RootId { get; init; }
}