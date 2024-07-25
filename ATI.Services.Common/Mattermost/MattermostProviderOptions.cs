#nullable enable
using System.Collections.Generic;
using JetBrains.Annotations;

namespace ATI.Services.Common.Mattermost;

[PublicAPI]
public class MattermostProviderOptions
{
    public required Dictionary<string, MattermostOptions> MattermostOptions { get; init; }
}