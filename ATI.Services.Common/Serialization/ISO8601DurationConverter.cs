using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml;
using JetBrains.Annotations;

namespace ATI.Services.Common.Serialization
{
    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// Сериализация/десериализация в/из TimeSpan представления вида "PT9M25.714S".
    /// </summary>
    [PublicAPI]
    public class ISO8601DurationConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();
            return XmlConvert.ToTimeSpan(s ?? throw new InvalidOperationException());
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(XmlConvert.ToString(value));
        }
    }
}