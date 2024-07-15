using System.Collections.Generic;

namespace ATI.Services.Common.Mattermost;

public class MattermostProviderOptions
{
    public Dictionary<string, MattermostOptions> MattermostOptions { get; set; }
}