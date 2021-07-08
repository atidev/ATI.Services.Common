using System;
using System.Collections.Generic;

namespace ATI.Services.Common.Sql
{
   public class DataBaseOptions
    {
        public string ConnectionString { get; set; }
        public TimeSpan Timeout { get; set; }
        public IDictionary<string, int> TimeoutDictionary { get; set; } = new Dictionary<string, int>();
        public TimeSpan? LongTimeRequest { get; set; }
    }
}
