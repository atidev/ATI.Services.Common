using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Http.Extensions;
using ATI.Services.Common.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;

namespace ATI.Services.Common.Mattermost;

public class MattermostAdapter
{
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly MattermostOptions _mattermostOptions;
    private readonly HttpClient _httpClient;
    
    private readonly JsonSerializerSettings _jsonSerializerSettings = new JsonSerializerSettings
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
    
    public MattermostAdapter(
        IHttpClientFactory httpClientFactory,
        MattermostOptions mattermostOptions)
    {
        _mattermostOptions = mattermostOptions;
        _httpClient = httpClientFactory.CreateClient(nameof(MattermostAdapter));
    }

    public async Task<OperationResult> SendAlertAsync(string text)
    {
        try
        {
            var url = _mattermostOptions.MattermostAddress + _mattermostOptions.WebHook;
            var payload = new MattermostWebHookRequestBody
            {
                Text = text,
                IconEmoji = _mattermostOptions.IconEmoji,
                Username = _mattermostOptions.UserName
            };
            // We do it, because mattermost api return text format instead of json
            var httpContent = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Content = new StringContent(JsonConvert.SerializeObject(payload, _jsonSerializerSettings),Encoding.UTF8, "application/json"),
                RequestUri = new Uri(url)
            };

            var httpResponse = await _httpClient.SendAsync(httpContent);
            if (!httpResponse.IsSuccessStatusCode)
            {
                _logger.ErrorWithObject(null, "Mattermost return error status code", new { request = httpContent, response = httpResponse });
                return new OperationResult(ActionStatus.InternalServerError);
            }

            return OperationResult.Ok;
        }
        catch (Exception e)
        {
            _logger.ErrorWithObject(e, "Something went wrong when try to send alert");   
            return new(ActionStatus.InternalServerError);
        }
    }
}