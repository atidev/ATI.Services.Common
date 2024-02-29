using System.Collections.Generic;

namespace ATI.Services.Common.Slack
{
    public class SlackProviderOptions
    {
        public Dictionary<string, SlackAdapterOptions> SlackOptions { get; set; }
    }
}