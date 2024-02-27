using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace ATI.Services.Common.Serializers.SystemTextJsonSerialization;

[PublicAPI]
public abstract class SystemTextJsonSerializerBase : ISerializer
{
    private JsonSerializerOptions _jsonSerializerOptions;

    public class SystemTextJsonSerializerWithCustomConverters : SystemTextJsonSerializerBase
    {
        public SystemTextJsonSerializerWithCustomConverters()
        {
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                Converters =
                {
                    new TimeSpanConverter(),
                    new DictionaryKeyValueConverter()
                }
            };
        }

        public override bool CanSerialize(string serializerType)
            => int.TryParse(serializerType, out var type) && type == (int)SerializerType.SystemTextJson;
    }

    public class SystemTextJsonSerializer : SystemTextJsonSerializerBase
    {
        public SystemTextJsonSerializer()
        {
            _jsonSerializerOptions = new JsonSerializerOptions();
        }

        public SystemTextJsonSerializer(JsonSerializerOptions serializerOptions)
        {
            _jsonSerializerOptions = serializerOptions;
        }

        public override bool CanSerialize(string serializerType) 
            => int.TryParse(serializerType, out var type) && type == (int)SerializerType.SystemTextJsonClassic;
    }

    public void SetSerializeSettings(object settings)
    {
        var set = (JsonSerializerOptions)settings;
        _jsonSerializerOptions = set;
    }

    public string Serialize<TIn>(TIn value)
    {
        return JsonSerializer.Serialize(value, _jsonSerializerOptions);
    }

    public string Serialize(object value, Type type)
    {
        return JsonSerializer.Serialize(value, type, _jsonSerializerOptions);
    }

    public T Deserialize<T>(string value)
    {
        return JsonSerializer.Deserialize<T>(value, _jsonSerializerOptions);
    }

    public object Deserialize(string value, Type type)
    {
        return JsonSerializer.Deserialize(value, type, _jsonSerializerOptions);
    }

    public async Task<T> DeserializeAsync<T>(Stream stream)
    {
        return await JsonSerializer.DeserializeAsync<T>(stream, _jsonSerializerOptions);
    }

    public abstract bool CanSerialize(string serializerType);
}