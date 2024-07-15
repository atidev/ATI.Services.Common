namespace ATI.Services.Common.Mattermost;

public class MattermostWebHookRequestBody
{
    public string Text { get; set; }
    public string Username { get; set; }
    public string IconEmoji { get; set; }
}