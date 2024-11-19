using Newtonsoft.Json;

namespace ATI.Services.Common.Behaviors;

public class FullErrorResponse : ErrorResponse
{
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public List<ErrorResponse> ErrorList { get; set; } = null!;

    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public IReadOnlyDictionary<string, object> Details { get; set; } = null!;
}