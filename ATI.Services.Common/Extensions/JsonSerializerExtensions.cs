using System.IO;
using JetBrains.Annotations;
using Newtonsoft.Json;

namespace ATI.Services.Common.Extensions
{
    [PublicAPI]
    public static class JsonSerializerExtensions
    {
        /// <inheritdoc cref="JsonSerializer.Serialize(TextWriter,object)"/>
        public static string Serialize<T>(this JsonSerializer serializer, T model)
        {
            using var textWriter = new StringWriter();
            serializer.Serialize(textWriter, model);
            return textWriter.ToString();
        }

        public static TResult CloneToChild<TResult, TSource>(this TSource input) where TResult : TSource
        {
            return JsonConvert.DeserializeObject<TResult>(JsonConvert.SerializeObject(input));
        }

        /// <summary>
        /// Десериализует json, указанный в <paramref name="json"/> в объект типа <typeparamref name="T"/>.
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="json"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Deserialize<T>(this JsonSerializer serializer, byte[] json)
        {
            using var jsonStream = new MemoryStream(json, false);
            using var streamReader = new StreamReader(jsonStream);
            using var jsonReader = new JsonTextReader(streamReader);
            var model = serializer.Deserialize<T>(jsonReader);
            return model;
        }

        /// <summary>
        /// Десериализует json, указанный в <paramref name="json"/> в объект типа <typeparamref name="T"/>.
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="json"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T Deserialize<T>(this JsonSerializer serializer, string json)
        {
            using var streamReader = new StringReader(json);
            using var jsonReader = new JsonTextReader(streamReader);
            var model = serializer.Deserialize<T>(jsonReader);

            return model;
        }

        /// <summary>
        /// Сериализует объект типа <typeparamref name="T"/> в массив байтов.
        /// </summary>
        /// <param name="serializer"></param>
        /// <param name="model"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static byte[] ToJsonBytes<T>(this JsonSerializer serializer, T model)
        {
            using var stream = new MemoryStream();
            using var streamWriter = new StreamWriter(stream);
            using var jsonWriter = new JsonTextWriter(streamWriter);
            serializer.Serialize(jsonWriter, model);
            streamWriter.Flush();
            return stream.ToArray();
        }
    }
}