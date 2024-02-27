using System;
using ATI.Services.Common.Serializers.Newtonsoft;
using static ATI.Services.Common.Serializers.SystemTextJsonSerialization.SystemTextJsonSerializerBase;

namespace ATI.Services.Common.Serializers;

[Obsolete("Use SerializerProvider instead")]
public static class SerializerFactory
{
    public static ISerializer GetSerializerByType(SerializerType type)
    {
        return type switch
        {
            SerializerType.Newtonsoft => new NewtonsoftSerializer(),
            SerializerType.SystemTextJson => new SystemTextJsonSerializerWithCustomConverters(),
            SerializerType.SystemTextJsonClassic => new SystemTextJsonSerializer(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}