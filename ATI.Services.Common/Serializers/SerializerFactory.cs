using System;
using ATI.Services.Common.Serializers.Newtonsoft;
using ATI.Services.Common.Serializers.SystemTextJsonSerialization;

namespace ATI.Services.Common.Serializers
{
    public static class SerializerFactory
    {
        public static ISerializer GetSerializerByType(SerializerType type)
        {
            return type switch
            {
                SerializerType.Newtonsoft => new NewtonsoftSerializer(),
                SerializerType.SystemTextJson => new SystemTextJsonSerializer(),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}