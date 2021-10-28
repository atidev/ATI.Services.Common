using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

namespace ATI.Services.Common.Serializers.SystemTextJsonSerialization
{
    /// <summary>
    /// https://github.com/dotnet/corefx/blob/master/src/System.Text.Json/tests/Serialization/CustomConverterTests.DictionaryKeyValueConverter.cs
    /// </summary>
    [PublicAPI]
    public sealed class DictionaryKeyValueConverter : JsonConverterFactory
    {
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
            if (keyType == typeof(string))
            {
                return false;
            }

            return true;
        }

        public override JsonConverter CreateConverter(Type type, JsonSerializerOptions options)
        {
            var keyType = type.GetGenericArguments()[0];
            var valueType = type.GetGenericArguments()[1];

            var converter = (JsonConverter) Activator.CreateInstance(
                typeof(DictionaryKeyValueConverterInner<,>).MakeGenericType(new Type[] {keyType, valueType}),
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
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    throw new JsonException();
                }

                var value = new Dictionary<TKey, TValue>();

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndArray)
                    {
                        return value;
                    }

                    var kv = _converter.Read(ref reader, typeToConvert, options);
                    value.Add(kv.Key, kv.Value);
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, Dictionary<TKey, TValue> value,
                JsonSerializerOptions options)
            {
                writer.WriteStartArray();

                foreach (var kvp in value)
                {
                    _converter.Write(writer, kvp, options);
                }

                writer.WriteEndArray();
            }
        }
    }
}