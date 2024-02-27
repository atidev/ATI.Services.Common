using ATI.Services.Common.Serializers.Newtonsoft;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using static ATI.Services.Common.Serializers.SystemTextJsonSerialization.SystemTextJsonSerializerBase;

namespace ATI.Services.Common.Serializers;

public static class SerializerExtensions
{
    [PublicAPI]
    public static void AddSerializers(this IServiceCollection services)
    {
        // Регистрируем сериализаторы
        services.AddSingleton<ISerializer, SystemTextJsonSerializerWithCustomConverters>();
        services.AddSingleton<ISerializer, SystemTextJsonSerializer>();
        services.AddSingleton<ISerializer, NewtonsoftSerializer>();

        // register serializer provider
        services.AddSingleton<SerializerProvider>();
    }
}