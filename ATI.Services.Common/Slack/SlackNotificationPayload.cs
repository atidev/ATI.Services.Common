namespace ATI.Services.Common.Slack
{
    internal class SlackNotificationPayload
    {
        public string Channel { get; set; }
        public string Username { get; set; }
        public string IconEmoji { get; set; }
        public string Text { get; set; }
    }
}