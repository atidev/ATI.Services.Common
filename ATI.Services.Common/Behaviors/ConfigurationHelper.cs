using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace ATI.Services.Common.Behaviors
{
    [PublicAPI]
    public static class ConfigurationHelper
    {
        public static int GetIntegerFromConfig(string key, int defaultValue)
        {
            var config = ConfigurationManager.AppSettings(key);
            return int.TryParse(config, out var value) ? value : defaultValue;
        }

        public static bool GetBoolFromConfig(string key, bool defaultValue)
        {
            var config = ConfigurationManager.AppSettings(key);
            return bool.TryParse(config, out var value) ? value : defaultValue;
        }

        public static string GetStringFromConfig(string key)
        {
            return ConfigurationManager.AppSettings(key);
        }

        public static TimeSpan GetTimeSpanFromConfig(string key, TimeSpan defaultValue)
        {
            var config = ConfigurationManager.AppSettings(key);
            return TimeSpan.TryParse(config, out var value) ? value : defaultValue;
        }
    }

    public static class ConfigurationHelper<TKey, TValue>
    {
        public static Dictionary<TKey, TValue> GetDictionary(string sectionName, Func<string, TKey> actionOnKey,
            Func<string, TValue> actionOnValue)
        {
            var section = ConfigurationManager.GetSection(sectionName);
            var dictionary = new Dictionary<TKey, TValue>();
            if (section != null)
            {
                foreach (var configurationSection in section.GetChildren())
                {
                    dictionary.Add(actionOnKey(configurationSection.Key), actionOnValue(configurationSection.Value));
                }
            }

            return dictionary;
        }
    }
}