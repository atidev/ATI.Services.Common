using Newtonsoft.Json;

namespace ATI.Services.Common.Slack
{
    internal class SlackNotificationPayload
    {
        public string Channel { get; set; }
        public string Username { get; set; }
        public string IconEmoji { get; set; }
        public string Text { get; set; }
        
        /// <summary>
        /// Parent message id
        /// </summary>
        [JsonProperty("thread_ts")]
        public string MessageId { get; set; }
    }
}