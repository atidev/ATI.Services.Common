using NLog.Targets;

namespace ATI.Services.Common.Logging.Configuration
{
    public class ConfigNetworkTarget
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public bool KeepConnection { get; set; } = true;
        public NetworkTargetOverflowAction OnOverflow { get; set; } = NetworkTargetOverflowAction.Split;
        public bool NewLine { get; set; } = true;
        public bool AddGeneralAttributes { get; set; } = true;
        public ConfigJsonAttribute[] Attributes { get; set; } = { };

    }
}