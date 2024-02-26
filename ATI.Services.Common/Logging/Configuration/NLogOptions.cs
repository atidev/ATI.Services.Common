namespace ATI.Services.Common.Logging.Configuration;

public class NLogOptions
{
    public bool ThrowExceptions { get; set; } = false;
    public string LoggingEnvironment { get; set; }
    public string ApplicationName { get; set; }
    public bool AddGeneralAttributes { get; set; } = true;

    public NLogVariable[] Variables { get; set; } = { };
    public ConfigJsonAttribute[] Attributes { get; set; } = { };
    public ConfigFileTarget[] FileTargets { get; set; } = { };
    public ConfigNetworkTarget[] NetworkTargets { get; set; } = { };
    public Rule[] Rules { get; set; } = { };
        
    public string[] LoggedRequestHeader { get; set; } = { };
}