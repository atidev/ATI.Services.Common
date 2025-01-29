#nullable enable
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NLog;

namespace ATI.Services.Common.Mattermost;

[PublicAPI]
public class MattermostProvider
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<string, MattermostAdapter> _configuredMattermostAdapter = [];

    public MattermostProvider(
        IOptions<MattermostProviderOptions> mattermostProviderOptions,
        IHttpClientFactory httpClientFactory)
    {
        foreach (var mattermostOptions in mattermostProviderOptions.Value.MattermostOptions)
        {
            _configuredMattermostAdapter.Add(mattermostOptions.Key, new MattermostAdapter(httpClientFactory, mattermostOptions.Value));
        }
    }

    public MattermostAdapter? GetMattermostAdapter(string mattermostChannel)
    {
        var mattermostAdapter = CollectionsMarshal.GetValueRefOrNullRef(_configuredMattermostAdapter, mattermostChannel);

        if (mattermostAdapter is null)
            _logger.Error("Mattermost adapter was not configured");

        return mattermostAdapter;
    }
}