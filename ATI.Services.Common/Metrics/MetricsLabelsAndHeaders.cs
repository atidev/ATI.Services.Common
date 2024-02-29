using System.Collections.Generic;

namespace ATI.Services.Common.Metrics
{
    public static class MetricsLabelsAndHeaders
    {
        public static Dictionary<string, string> LabelsStatic { get; set; }

        public static string[] UserLabels;
        public static string[] UserHeaders;
    }
}