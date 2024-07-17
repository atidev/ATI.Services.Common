using Newtonsoft.Json;

namespace ATI.Services.Common.Mattermost;

public class PostMessageResponse
{
    public string Id { get; set; }
    public string ChannelId { get; set; }
    public string Message { get; set; }
}