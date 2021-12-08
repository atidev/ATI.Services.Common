using System;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ATI.Services.Common.Serializers.Newtonsoft
{
    [PublicAPI]
    public class NewtonsoftSerializer : ISerializer
    {
        private JsonSerializerSettings _serializeSettings;

        public NewtonsoftSerializer()
        {
            _serializeSettings = new JsonSerializerSettings
            {
                // Для приватной логики (работа с редисом и тд) игнорируем ShouldSerialize для корректной работы
                ContractResolver = new DefaultContractResolver { IgnoreShouldSerializeMembers = true }
            };
        }

        public NewtonsoftSerializer(JsonSerializerSettings serializeSettings)
        {
            _serializeSettings = serializeSettings;
        }

        public void SetSerializeSettings(object settings)
        {
            var set = (JsonSerializerSettings) settings;
            _serializeSettings = set;
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
    }
}