using System.Globalization;
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
    }
}