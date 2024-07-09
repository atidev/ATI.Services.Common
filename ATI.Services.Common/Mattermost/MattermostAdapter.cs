using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Http.Extensions;
using ATI.Services.Common.Logging;
using Newtonsoft.Json;
using NLog;

namespace ATI.Services.Common.Mattermost;

public class MattermostAdapter
{
    private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
    private readonly MattermostOptions _mattermostOptions;
    private readonly HttpClient _httpClient;
    
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
            return await _httpClient.SendAsync<string>(
                HttpMethod.Post,
                url,
                new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"),
                "Mattermost");
        }
        catch (Exception e)
        {
            _logger.ErrorWithObject(e, "Something went wrong when try to send alert");   
            return new(ActionStatus.InternalServerError);
        }
    }
}