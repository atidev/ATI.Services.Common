using System.Collections.Generic;
using System.Net.Http;
using ATI.Services.Common.Logging;
using Microsoft.Extensions.Options;
using NLog;

namespace ATI.Services.Common.Mattermost;

public class MattermostProvider
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<string, MattermostAdapter> _configuredMattermostAdapter = new ();

    public MattermostProvider(
        IOptions<MattermostProviderOptions> mattermostProviderOptions,
        IHttpClientFactory httpClientFactory)
    {
        foreach (var mattermostOptions in mattermostProviderOptions.Value.MattermostOptions)
        {
            _configuredMattermostAdapter.Add(mattermostOptions.Key, new MattermostAdapter(httpClientFactory, mattermostOptions.Value));
        }
    }

    public MattermostAdapter GetMattermostAdapter(string mattermostChannel)
    {
        var isMattermostAdapterConfigured = _configuredMattermostAdapter.TryGetValue(mattermostChannel, out var mattermostAdapter);
        if (isMattermostAdapterConfigured)
            return mattermostAdapter;
        
        _logger.ErrorWithObject(null, "Mattermost adapter was not configured");
        return null;
    }
}