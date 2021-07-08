using System.Collections.Generic;

namespace ATI.Services.Common.ServiceVariables
{
    public static class ServiceVariables
    {
        public static string ServiceAsClientName { get; set; }
        public static string ServiceAsClientHeaderName { get; set; }
        public static Dictionary<string, string> Variables { get; set; }
    }
}
