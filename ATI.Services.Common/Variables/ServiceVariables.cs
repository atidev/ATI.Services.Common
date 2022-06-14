using System.Collections.Generic;

namespace ATI.Services.Common.Variables
{
    public static class ServiceVariables
    {
        public static string ServiceAsClientName { get; set; }
        public static string ServiceAsClientHeaderName { get; set; }
        public static string DefaultLocale { get; set; }
        public static string[] SupportedLocales { get; set; }
        public static Dictionary<string, string> Variables { get; set; }
    }
}