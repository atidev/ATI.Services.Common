using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Http.Extensions;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Metrics.HttpWrapper;
using ATI.Services.Common.Serializers;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;

namespace ATI.Services.Common.Mattermost;

public class MattermostAdapter(
    IHttpClientFactory httpClientFactory,
    MattermostOptions mattermostOptions)
{
    private const string MattermostPostMessageUrl = "/api/v1/posts";
    public const string HttpClientName = nameof(MattermostAdapter);

    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    private readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        ContractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy
            {
                ProcessDictionaryKeys = true,
                OverrideSpecifiedNames = true
            }
        },
        NullValueHandling = NullValueHandling.Include
    };

    public async Task<OperationResult> SendAlertAsync(string text)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient(HttpClientName);
            var url = mattermostOptions.MattermostAddress + mattermostOptions.WebHook;
            var payload = new MattermostWebHookRequestBody
            {
                Text = text,
                IconEmoji = mattermostOptions.IconEmoji,
                Username = mattermostOptions.UserName
            };
            // We do it, because mattermost api return text format instead of json
            using var httpContent = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(payload, _jsonSerializerSettings),
                    Encoding.UTF8, "application/json"),
                RequestUri = new Uri(url)
            };

            using var httpResponse = await httpClient.SendAsync(httpContent);

            if (httpResponse.IsSuccessStatusCode)
                return OperationResult.Ok;

            var errResponse = $"Mattermost return error status code {httpResponse.StatusCode}";
            _logger.ErrorWithObject(null, errResponse, new { request = httpContent, response = httpResponse });
            return new OperationResult(ActionStatus.ExternalServerError, errResponse);
        }
        catch (Exception e)
        {
            _logger.ErrorWithObject(e, "Something went wrong when try to send alert");
            return new(e);
        }
    }

    public async Task<OperationResult<PostMessageResponse>> SendMessageAsync(string message, string channelId)
    {
        var payload = new MattermostNotificationPayload
        {
            Message = message,
            ChannelId = channelId
        };
        return await SendBotMessageAsync(payload);
    }

    public async Task<OperationResult<PostMessageResponse>> SendThreadMessageAsync(string message, string channelId,
        string messageId)
    {
        var payload = new MattermostNotificationPayload
        {
            ChannelId = channelId,
            RootId = messageId,
            Message = message,
        };
        return await SendBotMessageAsync(payload);
    }

    private async Task<OperationResult<PostMessageResponse>> SendBotMessageAsync(MattermostNotificationPayload payload)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient(HttpClientName);
            var url = mattermostOptions.MattermostAddress + MattermostPostMessageUrl;

            using var httpContent = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(payload, _jsonSerializerSettings),
                    Encoding.UTF8, "application/json"),
                RequestUri = new Uri(url),
                Headers =
                {
                    Authorization = new AuthenticationHeaderValue("Bearer", mattermostOptions.BotAccessToken)
                }
            };

            using var httpResponse = await httpClient.SendAsync(httpContent);
            if (!httpResponse.IsSuccessStatusCode)
            {
                const string errMsg = "Mattermost return error status code";
                var responseErr = await httpResponse.Content.ReadAsStringAsync();
                _logger.ErrorWithObject(null, errMsg, new { request = httpContent, response = responseErr });
                return new(ActionStatus.ExternalServerError, responseErr);
            }

            var ans = await httpResponse.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<PostMessageResponse>(ans, _jsonSerializerSettings);

            return new(result);
        }
        catch (Exception e)
        {
            _logger.ErrorWithObject(e, "Something went wrong when try to send alert");
            return new(e);
        }
    }
}