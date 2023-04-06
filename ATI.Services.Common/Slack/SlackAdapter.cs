using System;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Metrics;
using ATI.Services.Common.Metrics.ExternalHttpWrapper;
using ATI.Services.Common.Serializers;
using JetBrains.Annotations;
using NLog;

namespace ATI.Services.Common.Slack
{
    /// <summary>
    /// Adapter use Slack Incoming Webhooks 
    /// </summary>
    [PublicAPI]
    public class SlackAdapter
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly MetricsExternalHttpClientWrapper _externalHttpClient;
        private const string ServiceName = "slack";
        private readonly SlackAdapterOptions _slackOptions;

        public SlackAdapter(SlackAdapterOptions options)
        {
            _slackOptions = options;
            var config = new MetricsExternalHttpClientConfig(ServiceName, TimeSpan.FromSeconds(5), SerializerType.Newtonsoft);
            _externalHttpClient = new MetricsExternalHttpClientWrapper(config);
        }
        
        public async Task<OperationResult> SendAlertAsync(string alert)
        {
            try
            {
                if (!_slackOptions.AlertsEnabled)
                    return new OperationResult(ActionStatus.Ok);

                var payload = new SlackNotificationPayload
                {
                    IconEmoji = _slackOptions.Emoji,
                    Channel = _slackOptions.AlarmChannel,
                    Text = alert,
                    Username = _slackOptions.BotName
                };
                var response = await _externalHttpClient.PostAsync(
                    _slackOptions.SlackAddress,
                    "SlackAlert",
                    _slackOptions.WebHookUri,
                    payload);

                return response.Success
                    ? new OperationResult(ActionStatus.Ok)
                    : new OperationResult(response);
            }
            catch (Exception e)
            {
                _logger.ErrorWithObject(e, new { Alert = alert });
                return new OperationResult(e);
            }
        }
    }
}