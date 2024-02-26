namespace ATI.Services.Common.Logging.Configuration;

public class ConfigJsonAttribute
{
    public string Name { get; set; }
    public string Layout { get; set; }
    public bool EscapeUnicode { get; set; } = false;
    public bool EncodeJson { get; set; } = true;
    public bool IncludeEmptyValue { get; set; } = false;
}