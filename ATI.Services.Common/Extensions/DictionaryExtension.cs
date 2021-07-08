using System.Collections.Generic;
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
    }
}
