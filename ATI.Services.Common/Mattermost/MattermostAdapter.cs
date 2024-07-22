#nullable enable
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Logging;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;

namespace ATI.Services.Common.Mattermost;

/// <summary>
/// Адаптер для интеграции с Mattermost
/// </summary>
[PublicAPI]
public class MattermostAdapter(
    IHttpClientFactory httpClientFactory,
    MattermostOptions mattermostOptions)
{
    private const string MattermostPostMessageUrl = "/api/v4/posts";
    private const string DefaultIconEmoji = ":upside_down_face:";
    private const string DefaultBotName = "Nameless Bot";
    public const string HttpClientName = nameof(MattermostAdapter);
    
    private static readonly JsonSerializerSettings JsonSerializerSettings = new()
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

    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Отправляет сообщение используя webhook
    /// </summary>
    /// <param name="text">Текст сообщения (поддерживает markdown)</param>
    public async Task<OperationResult> SendAlertAsync(string text)
    {
        if (mattermostOptions.WebHook is null)
            return new(ActionStatus.LogicalError, "Please setup WebHook in MattermostOptions");

        try
        {
            using var httpClient = httpClientFactory.CreateClient(HttpClientName);
            var url = mattermostOptions.MattermostAddress + mattermostOptions.WebHook;
            var payload = new MattermostWebHookRequestBody
            {
                Text = text,
                IconEmoji = mattermostOptions.IconEmoji ?? DefaultIconEmoji,
                Username = mattermostOptions.UserName ?? DefaultBotName
            };

            // We do it, because mattermost api return text format instead of json
            using var httpContent = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(payload, JsonSerializerSettings),
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

    /// <summary>
    /// Отправить сообщение в канал используя интеграцию через бота 
    /// </summary>
    /// <param name="message">Текст сообщения (поддерживает markdown)</param>
    /// <param name="channelId">Id канала</param>
    public async Task<OperationResult<PostMessageResponse>> SendMessageAsync(string message, string channelId)
    {
        var payload = new MattermostNotificationPayload
        {
            Message = message,
            ChannelId = channelId
        };
        return await SendBotMessageAsync(payload);
    }

    /// <summary>
    /// Отправить сообщение в тред
    /// </summary>
    /// <param name="message">Текст сообщения</param>
    /// <param name="channelId">Id канала</param>
    /// <param name="rootId">Id корневого сообщения (первого сообщения в треде)</param>
    /// <returns></returns>
    public async Task<OperationResult<PostMessageResponse>> SendThreadMessageAsync(
        string message,
        string channelId,
        string rootId
    )
    {
        var payload = new MattermostNotificationPayload
        {
            ChannelId = channelId,
            RootId = rootId,
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
                Content = new StringContent(JsonConvert.SerializeObject(payload, JsonSerializerSettings),
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
                var responseErr = await httpResponse.Content.ReadAsStringAsync();
                _logger.ErrorWithObject(null, "Mattermost return error status code", new { request = httpContent, response = responseErr });
                return new(ActionStatus.ExternalServerError, responseErr);
            }

            var ans = await httpResponse.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<PostMessageResponse>(ans, JsonSerializerSettings);

            return result is not null 
                ? new(result) 
                : new (ActionStatus.LogicalError, $"Не удалось десериализовать ответ. Тело: {ans}");
        }
        catch (Exception e)
        {
            _logger.ErrorWithObject(e, "Something went wrong when try to send alert");
            return new(e);
        }
    }
}