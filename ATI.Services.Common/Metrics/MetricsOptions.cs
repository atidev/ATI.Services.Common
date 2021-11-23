using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;

namespace ATI.Services.Common.Metrics
{
    public class MetricsOptions
    {
        public Dictionary<string, string> Labels { get; set; }

        internal static Dictionary<string, string> LabelsStatic { get; set; }
        
        public static string[] UserLabels => LabelsStatic.Keys.ToArray();
        public static string[] UserHeaders => LabelsStatic.Values.ToArray();
    }
}