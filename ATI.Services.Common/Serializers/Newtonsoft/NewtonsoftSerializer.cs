using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ATI.Services.Common.Serializers.Newtonsoft
{
    [PublicAPI]
    public class NewtonsoftSerializer : ISerializer
    {
        private JsonSerializerSettings _serializeSettings;
        private JsonSerializer _serializer;

        public NewtonsoftSerializer()
        {
            _serializeSettings = new JsonSerializerSettings
            {
                // Для приватной логики (работа с редисом и тд) игнорируем ShouldSerialize для корректной работы
                ContractResolver = new DefaultContractResolver { IgnoreShouldSerializeMembers = true }
            };
            SetJsonSerializer();
        }
        
        public NewtonsoftSerializer(JsonSerializerSettings settings)
        {
            _serializeSettings = settings;
            SetJsonSerializer();
        }

        public void SetSerializeSettings(object settings)
        {
            var set = (JsonSerializerSettings) settings;
            _serializeSettings = set;
            SetJsonSerializer();
        }

        public string Serialize<T>(T value)
        {
            return JsonConvert.SerializeObject(value, _serializeSettings);
        }

        public string Serialize(object value, Type type)
        {
            return JsonConvert.SerializeObject(value, type, _serializeSettings);
        }

        public T Deserialize<T>(string value)
        {
            return JsonConvert.DeserializeObject<T>(value, _serializeSettings);
        }

        public object Deserialize(string value, Type type)
        {
            return JsonConvert.DeserializeObject(value, type, _serializeSettings);
        }

        public Task<T> DeserializeAsync<T>(Stream stream)
        {
            using var streamReader = new StreamReader(stream);
            using var jsonReader = new JsonTextReader(streamReader);
            return Task.FromResult(_serializer.Deserialize<T>(jsonReader));
        }

        private void SetJsonSerializer()
        {
            _serializer = JsonSerializer.Create(_serializeSettings);
        }
    }
}