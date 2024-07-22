#nullable enable
using JetBrains.Annotations;

namespace ATI.Services.Common.Mattermost;

/// <summary>
/// Настройки Mattermost адаптера
/// </summary>
[PublicAPI]
public class MattermostOptions
{
    public required string MattermostAddress { get; init; }

    #region HookIntegration

    public string? UserName { get; init; }
    public string? IconEmoji { get; init; }
    public string? WebHook { get; init; }

    #endregion

    /// <summary>
    /// Bearer Access Token для интеграции через бота
    /// </summary>
    public string? BotAccessToken { get; init; }
    
    /// <summary>
    /// Канал для отправки уведомлений
    /// </summary>
    public string? AlarmChannel { get; init; }
}