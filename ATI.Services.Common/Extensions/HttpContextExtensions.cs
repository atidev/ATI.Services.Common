using System.Linq;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace ATI.Services.Common.Extensions;

[PublicAPI]
public static class HttpContextExtensions
{
    public static bool NeedTransliterationByAcceptLanguage(this HttpContext context)
    {
        var parsedCultures = context.Request.GetTypedHeaders().AcceptLanguage;
        var enCulture = parsedCultures.FirstOrDefault(c => c.Value == "en");
        var ruCulture = parsedCultures.FirstOrDefault(c => c.Value == "ru");
        
        if (enCulture is null)
            return false;
            
        if (ruCulture is null || ruCulture.Quality < enCulture.Quality)
            return true;

        return false;
    }
}