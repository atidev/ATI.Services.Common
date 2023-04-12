using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Metrics;
using ATI.Services.Common.Metrics.HttpWrapper;
using ATI.Services.Common.Serializers;
using JetBrains.Annotations;
using NLog;

namespace ATI.Services.Common.Slack;

/// <summary>
/// Adapter use Slack API
/// </summary>
[PublicAPI]
public class SlackApiAdapter
{
    private const string SlackApiBaseAddress = "https://slack.com";
    private const string SlackPostMessageUrl = "/api/chat.postMessage";
    private const string SlackApiMetric = "SlackApi";
    private const string ServiceName = "Slack";

    private readonly MetricsHttpClientWrapper _httpClient;
    private readonly SlackAdapterOptions _slackOptions;

    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    public SlackApiAdapter(SlackAdapterOptions options)
    {
        _slackOptions = options;
        var config = new MetricsHttpClientConfig(ServiceName, TimeSpan.FromSeconds(5), SerializerType.Newtonsoft);
        _httpClient = new MetricsHttpClientWrapper(config);
    }

    /// <summary>
    /// Отправляет сообщение в канал
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    public async Task<OperationResult<PostMessageResponse>> SendMessageAsync(string message)
    {
        return await PostAsync<SlackNotificationPayload, PostMessageResponse>(
            SlackPostMessageUrl,
            new SlackNotificationPayload
            {
                IconEmoji = _slackOptions.Emoji,
                Channel = _slackOptions.AlarmChannel,
                Text = message,
                Username = _slackOptions.BotName
            });
    }

    /// <summary>
    /// Отправляет текст в тред сообщения
    /// </summary>
    /// <param name="message">Текст сообщения отправляемого в тред</param>
    /// <param name="messageId">Идентификатор родительского сообщения</param>
    /// <returns></returns>
    public async Task<OperationResult<PostMessageResponse>> SendThreadMessageAsync(string message, string messageId)
    {
        return await PostAsync<SlackNotificationPayload, PostMessageResponse>(
            SlackPostMessageUrl,
            new SlackNotificationPayload
            {
                IconEmoji = _slackOptions.Emoji,
                Channel = _slackOptions.AlarmChannel,
                Text = message,
                Username = _slackOptions.BotName,
                MessageId = messageId
            });
    }
    
    private async Task<OperationResult<TResponse>> PostAsync<TRequest, TResponse>(string apiTemplate,
        TRequest requestBody) where TResponse : new()
    {
        try
        {
            if (!_slackOptions.AlertsEnabled)
                return new OperationResult<TResponse>(new TResponse());

            var headers = new Dictionary<string, string> {{"Authorization", $"Bearer {_slackOptions.BotAccessToken}"}};

            return await _httpClient.PostAsync<TRequest, TResponse>(
                SlackApiBaseAddress, SlackApiMetric, apiTemplate, requestBody, headers);
        }
        catch (Exception e)
        {
            _logger.ErrorWithObject(e, new {Body = requestBody});
            return new OperationResult<TResponse>(e);
        }
    }
}