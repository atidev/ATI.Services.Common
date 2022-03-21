using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace ATI.Services.Common.Serializers.SystemTextJsonSerialization
{
    [PublicAPI]
    public sealed class DictionaryKeyValueConverter : JsonConverterFactory
    {
        private static readonly HashSet<Type> ValueTupleTypes = new(new[]
        {
            typeof(ValueTuple<>),
            typeof(ValueTuple<,>),
            typeof(ValueTuple<,,>),
            typeof(ValueTuple<,,,>),
            typeof(ValueTuple<,,,,>),
            typeof(ValueTuple<,,,,,>),
            typeof(ValueTuple<,,,,,,>),
            typeof(ValueTuple<,,,,,,,>)
        });
        
        public override bool CanConvert(Type typeToConvert)
        {
            if (!typeToConvert.IsGenericType)
            {
                return false;
            }

            if (typeToConvert.GetGenericTypeDefinition() != typeof(Dictionary<,>))
            {
                return false;
            }

            // Don't change semantics of Dictionary<string, TValue> which uses JSON properties (not array of KeyValuePairs).
            var keyType = typeToConvert.GetGenericArguments()[0];

            if (keyType.IsEnum)
            {
                return false;
            }

            if (keyType.IsGenericType && ValueTupleTypes.Contains(keyType.GetGenericTypeDefinition()))
            {
                return false;
            }

            return keyType != typeof(string);
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            var keyType = type.GetGenericArguments()[0];
            var valueType = type.GetGenericArguments()[1];

            var converter = (JsonConverter) Activator.CreateInstance(
                typeof(DictionaryKeyValueConverterInner<,>).MakeGenericType(keyType, valueType),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[] {options},
                culture: null);

            return converter;
        }

        private class DictionaryKeyValueConverterInner<TKey, TValue> : JsonConverter<Dictionary<TKey, TValue>>
        {
            private readonly JsonConverter<KeyValuePair<TKey, TValue>> _converter;

            public DictionaryKeyValueConverterInner(JsonSerializerOptions options)
            {
                _converter =
                    (JsonConverter<KeyValuePair<TKey, TValue>>) options.GetConverter(
                        typeof(KeyValuePair<TKey, TValue>));
            }

            public override Dictionary<TKey, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert,
                JsonSerializerOptions options)
            {
                var dictionaryWithStringKey = (Dictionary<string, TValue>)JsonSerializer.Deserialize(ref reader, typeof(Dictionary<string, TValue>), options);
                
                var dictionary = new Dictionary<TKey, TValue>();

                foreach (var (key, value) in dictionaryWithStringKey)
                {
                    dictionary.Add((TKey)Convert.ChangeType(key, typeof(TKey)), value);
                }

                return dictionary;
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<TKey, TValue> value,
                JsonSerializerOptions options)
            {
                var dictionary = new Dictionary<string, TValue>(value.Count);

                foreach (var (key, value1) in value)
                {
                    dictionary.Add(key.ToString(), value1);
                }
                
                JsonSerializer.Serialize(writer, dictionary, options);
            }
        }
    }
}
