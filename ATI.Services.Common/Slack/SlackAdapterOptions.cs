namespace ATI.Services.Common.Slack
{
    public class SlackAdapterOptions
    {
        public bool AlertsEnabled { get; set; }
        public string AlarmChannel { get; set; }
        public string BotName { get; set; }
        public string Emoji { get; set; }
        public string SlackAddress { get; set; }
        public string WebHookUri { get; set; }
    }
}