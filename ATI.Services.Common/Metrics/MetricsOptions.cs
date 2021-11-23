using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace ATI.Services.Common.Metrics
{
    public static class MetricsOptions
    {
        public static Dictionary<string, string> Labels { get; set; }

        public static string[] UserLabels => Labels.Keys.ToArray();
        public static string[] UserHeaders => Labels.Values.ToArray();
    }
}