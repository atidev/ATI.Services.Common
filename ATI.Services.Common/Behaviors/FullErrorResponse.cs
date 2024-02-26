using System.Collections.Generic;
using Newtonsoft.Json;

namespace ATI.Services.Common.Behaviors;

public class FullErrorResponse : ErrorResponse
{
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public List<ErrorResponse> ErrorList { get; set; }
        
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Dictionary<string, object> Details { get; set; }
}