using System;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Tracing;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;

namespace ATI.Services.Common.Slack
{
    [PublicAPI]
    public class SlackAdapter
    {
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private readonly TracingHttpClientWrapper _httpClient;
        private const string ServiceName = "slack";
        private readonly SlackAdapterOptions _slackOptions;

        public SlackAdapter(SlackAdapterOptions options)
        {
            _slackOptions = options;
            _httpClient = new TracingHttpClientWrapper(new TracedHttpClientConfig
            {
                ServiceName = ServiceName,
                Serializer = new JsonSerializer
                {
                    ContractResolver = new DefaultContractResolver
                    {
                        NamingStrategy = new SnakeCaseNamingStrategy
                        {
                            OverrideSpecifiedNames = true,
                            ProcessDictionaryKeys = true
                        }
                    }
                },
                Timeout = TimeSpan.FromSeconds(5)
            });
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
                var response = await _httpClient.PostAsync(
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
                return new OperationResult(ActionStatus.InternalServerError);
            }
        }
    }
}