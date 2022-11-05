using Newtonsoft.Json;

namespace ATI.Services.Common.Slack;

public class PostMessageResponse
{
    public bool Ok { get; set; }
    public string Channel { get; set; }
    [JsonProperty("ts")]
    public string MessageId { get; set; }
}