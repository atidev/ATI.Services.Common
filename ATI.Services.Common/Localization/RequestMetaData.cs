using JetBrains.Annotations;

namespace ATI.Services.Common.Localization;

[PublicAPI]
public class RequestMetaData
{
    public string RequestAcceptLanguage { get; set; }
    public string RabbitAcceptLanguage { get; set; }
    public string CustomAcceptLanguage { get; set; }

    public string AcceptLanguage => CustomAcceptLanguage ?? RequestAcceptLanguage ?? RabbitAcceptLanguage ?? "ru";
}