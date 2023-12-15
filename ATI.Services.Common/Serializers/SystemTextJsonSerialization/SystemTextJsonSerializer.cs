using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using ATI.Services.Common.Metrics;
using JetBrains.Annotations;

namespace ATI.Services.Common.Serializers.SystemTextJsonSerialization
{
    [PublicAPI]
    public class SystemTextJsonSerializer : ISerializer
    {
        private JsonSerializerOptions _jsonSerializerOptions;

        public SystemTextJsonSerializer(bool enableCustomConverters = true)
        {
            _jsonSerializerOptions = enableCustomConverters
                ? new JsonSerializerOptions
                {
                    Converters =
                    {
                        new TimeSpanConverter(),
                        new DictionaryKeyValueConverter()
                    },
                    TypeInfoResolver = new DefaultJsonTypeInfoResolver()
                    {
                        Modifiers = { IgnoreUserSensitiveData }
                    }
                } : new JsonSerializerOptions();
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

        public async Task<T> DeserializeAsync<T>(Stream stream)
        {
            return await JsonSerializer.DeserializeAsync<T>(stream, _jsonSerializerOptions);
        }
        
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
}