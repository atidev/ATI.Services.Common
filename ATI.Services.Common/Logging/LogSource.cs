using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace ATI.Services.Common.Logging
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum LogSource
    {
        DefaultOrTest = 0,
        HttpClient = 1,
        Redis = 2,
        Mongo = 3,
        Sql = 4,
        Controller = 5, 
        Repository = 6,
        Tracing = 7
    }
}
