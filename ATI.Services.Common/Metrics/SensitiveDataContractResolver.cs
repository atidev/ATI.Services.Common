using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ATI.Services.Common.Metrics;

/// <summary>
/// Позволяет убирать из сериализации поля, помеченные атрибутом UserSensitiveDataAttribute
/// </summary>
public class SensitiveDataContractResolver : DefaultContractResolver
{
    public static SensitiveDataContractResolver Instance { get; } = new();

    private SensitiveDataContractResolver()
    {
        IgnoreShouldSerializeMembers = true;
    }

    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        JsonProperty property = base.CreateProperty(member, memberSerialization);
        var attribute = member.GetCustomAttributes<UserSensitiveDataAttribute>(false);
        if (attribute.Any())
        {
            property.Ignored = true;
        }

        return property;
    }
}