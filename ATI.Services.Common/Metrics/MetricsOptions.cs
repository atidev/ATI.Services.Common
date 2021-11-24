using System.Collections.Generic;

namespace ATI.Services.Common.Metrics
{
    public class MetricsOptions
    {
        /// <summary>
        /// User's additioanal labels and headers
        /// Keys are labels
        /// Values are headers
        /// </summary>
        public Dictionary<string, string> LabelsAndHeaders { get; set; }
    }
}