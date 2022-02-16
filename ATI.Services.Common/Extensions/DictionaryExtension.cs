using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace ATI.Services.Common.Extensions
{
    [PublicAPI]
    public static class DictionaryExtension
    {
        public static bool TryGetValue<T>(this IDictionary<string, string> dictionary, string key, out T value)
        {
            value = default;
            return dictionary.TryGetValue(key, out var strValue) && strValue.TryConvert(out value);
        }
        
        public static void AddRange<T, S>(this Dictionary<T, S> source, Dictionary<T, S> collection)
        {
            if (collection == null || collection.Count == 0)
                return;

            foreach (var (key, value) in collection.Where(item => !source.ContainsKey(item.Key)))
            {
                source.Add(key, value);
            }
        }
    }
}
