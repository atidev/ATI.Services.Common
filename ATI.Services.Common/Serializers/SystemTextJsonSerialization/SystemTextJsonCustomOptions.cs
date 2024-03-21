using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ATI.Services.Common.Logging;

namespace ATI.Services.Common.Serializers.SystemTextJsonSerialization;

public static class SystemTextJsonCustomOptions
{
    public static readonly JsonSerializerOptions IgnoreUserSensitiveDataOptions = new()
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { IgnoreUserSensitiveData }
        },
        // https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/character-encoding#serialize-all-characters
        // dont escape html-tags, cyrillic 
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    
    private static void IgnoreUserSensitiveData(JsonTypeInfo typeInfo)
    {
        foreach (var propertyInfo in typeInfo.Properties)
        {
            if (propertyInfo.AttributeProvider != null && propertyInfo.AttributeProvider.IsDefined(typeof(UserSensitiveDataAttribute), false))
            {
                propertyInfo.ShouldSerialize = (_, _) => false;
            }
        }
    }
}