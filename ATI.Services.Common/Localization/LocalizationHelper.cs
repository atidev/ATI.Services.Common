using System;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using ATI.Services.Common.Variables;
using JetBrains.Annotations;

namespace ATI.Services.Common.Localization
{
    [PublicAPI]
    public static class LocalizationHelper
    {
        public static string GetLocale()
        {
            return CultureInfo.CurrentCulture.Name;
        }

        public static CultureInfo GetFromString(string acceptLanguage)
        {
            var language =
                acceptLanguage.Split(',')
                    .Select(StringWithQualityHeaderValue.Parse)
                    .Where(lang => ServiceVariables.SupportedLocales.Contains(lang.Value, StringComparer.OrdinalIgnoreCase))
                    .MaxBy(lang => lang.Quality);


            return language != null ? new CultureInfo(language.Value) : null;
        }
    }
}