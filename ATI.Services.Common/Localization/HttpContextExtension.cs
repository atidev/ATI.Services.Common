using System.Globalization;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace ATI.Services.Common.Localization
{
    public static class HttpContextExtension
    {
        /// <summary>
        /// Пользователь может быть незалогиненным, но язык у него будет
        /// </summary>
        /// <param name="requestContext"></param>
        /// <returns></returns>
        [PublicAPI]
        public static string GetUserLanguage(this HttpContext requestContext)
        {
            return CultureInfo.CurrentCulture.Name;
        }
    }
}