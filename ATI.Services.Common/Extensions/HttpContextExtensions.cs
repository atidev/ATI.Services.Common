using System.Linq;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace ATI.Services.Common.Extensions;

[PublicAPI]
public static class HttpContextExtensions
{
    public static bool IsNeedTransliterationByAcceptLanguage(this HttpContext context)
    {
        var parsedCultures = context.Request.GetTypedHeaders().AcceptLanguage;
        var enCulture = parsedCultures.FirstOrDefault(c => c.Value == "en");
        var ruCulture = parsedCultures.FirstOrDefault(c => c.Value == "ru");

        return enCulture is not null && (ruCulture is null || ruCulture.Quality < enCulture.Quality);
    }
}