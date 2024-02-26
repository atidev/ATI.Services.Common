using System.Collections.Generic;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NLog;

namespace ATI.Services.Common.Slack;

[PublicAPI]
public class SlackProvider
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly Dictionary<string, SlackAdapter> _configuredSlackAdapters = new();

    public SlackProvider(IOptions<SlackProviderOptions> slackProviderOptions)
    {
        foreach (var slackOptions in slackProviderOptions.Value.SlackOptions)
        {
            _configuredSlackAdapters.Add(slackOptions.Key, new SlackAdapter(slackOptions.Value));
        }
    }

    public SlackAdapter GetAdapter(string slackChannel)
    {
        var isSlackAdapterConfigured = _configuredSlackAdapters.TryGetValue(slackChannel, out var slackAdapter);
        if (isSlackAdapterConfigured)
        {
            return slackAdapter;
        }
        _logger.Error($"В пуле нет SlackAdapter с каналом = {slackChannel}");
        return null;
    }
}