using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using ATI.Services.Common.Logging;

namespace ATI.Services.Common.Serializers.SystemTextJsonSerialization;

public static class SystemTextJsonCustomOptions
{
    public static JsonSerializerOptions IgnoreUserSensitiveDataOptions = new JsonSerializerOptions
    {
        TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { IgnoreUserSensitiveData }
        }
    };
    
    private static void IgnoreUserSensitiveData(JsonTypeInfo typeInfo)
    {
        foreach (var propertyInfo in typeInfo.Properties)
        {
            if (propertyInfo.AttributeProvider != null && propertyInfo.AttributeProvider.IsDefined(typeof(UserSensitiveDataAttribute), true))
            {
                propertyInfo.ShouldSerialize = (_, _) => false;
            }
        }
    }
}