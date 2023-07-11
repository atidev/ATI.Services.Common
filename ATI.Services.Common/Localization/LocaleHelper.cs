using System;
using System.Globalization;
using System.Linq;
using System.Net.Http.Headers;
using ATI.Services.Common.Variables;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace ATI.Services.Common.Localization
{
    [PublicAPI]
    public static class LocaleHelper
    {
        public static string GetLocale(bool withDefaultLocale = true)
        {
            return !string.IsNullOrEmpty(CultureInfo.CurrentUICulture.Name)
                ? CultureInfo.CurrentUICulture.Name
                : withDefaultLocale
                    ? ServiceVariables.DefaultLocale
                    : CultureInfo.CurrentUICulture.Name;
        }

        public static string GetUserLanguage(this HttpContext requestContext, bool withDefaultLocale = true)
        {
            return GetLocale(withDefaultLocale);
        }

        public static bool TryGetFromString(string acceptLanguage, out CultureInfo cultureInfo)
        {
            cultureInfo = null;
            try
            {
                var language =
                    acceptLanguage.Split(',')
                        .Select(StringWithQualityHeaderValue.Parse)
                        .Where(lang =>
                            ServiceVariables.SupportedLocales.Contains(lang.Value, StringComparer.OrdinalIgnoreCase))
                        .MaxBy(lang => lang.Quality);

                if (language == null) 
                    return false;
                
                cultureInfo = new CultureInfo(language.Value);
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}