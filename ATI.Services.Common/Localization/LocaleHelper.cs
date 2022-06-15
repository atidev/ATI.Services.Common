using System;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using ATI.Services.Common.Variables;
using JetBrains.Annotations;

namespace ATI.Services.Common.Localization
{
    [PublicAPI]
    public static class LocaleHelper
    {
        public static string GetLocale(bool withDefaultLocale = true)
        {
            return !string.IsNullOrEmpty(CultureInfo.CurrentCulture.Name)
                ? CultureInfo.CurrentCulture.Name
                : withDefaultLocale
                    ? ServiceVariables.DefaultLocale
                    : CultureInfo.CurrentCulture.Name;
        }

        public static CultureInfo GetFromString(string acceptLanguage)
        {
            var language =
                acceptLanguage.Split(',')
                    .Select(StringWithQualityHeaderValue.Parse)
                    .Where(lang =>
                        ServiceVariables.SupportedLocales.Contains(lang.Value, StringComparer.OrdinalIgnoreCase))
                    .MaxBy(lang => lang.Quality);


            return language != null ? new CultureInfo(language.Value) : null;
        }
    }
}