using System;
using System.Text.Json;
using JetBrains.Annotations;

namespace ATI.Services.Common.Serializers.SystemTextJsonSerialization
{
    [PublicAPI]
    public class SystemTextJsonSerializer : ISerializer
    {
        private JsonSerializerOptions _jsonSerializerOptions;

        public SystemTextJsonSerializer(bool disableConverters = false)
        {
            _jsonSerializerOptions = disableConverters
                ? new JsonSerializerOptions()
                : new JsonSerializerOptions
                {
                    Converters =
                    {
                        new TimeSpanConverter(),
                        new DictionaryKeyValueConverter()
                    }
                };
        }

        public SystemTextJsonSerializer(JsonSerializerOptions serializerOptions)
        {
            _jsonSerializerOptions = serializerOptions;
        }

        public void SetSerializeSettings(object settings)
        {
            var set = (JsonSerializerOptions) settings;
            _jsonSerializerOptions = set;
        }

        public string Serialize<T>(T value)
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
    }
}