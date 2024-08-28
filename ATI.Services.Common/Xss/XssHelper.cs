using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AngleSharp.Css.Dom;
using Ganss.Xss;
using Microsoft.AspNetCore.Http;

namespace ATI.Services.Common.Xss;

public static class XssHelper
{
    private static readonly HtmlSanitizer DefaultSanitizer = new();

    private static readonly HtmlSanitizer StrictSanitizer = new(new HtmlSanitizerOptions()
    {
        AllowedTags = new HashSet<string>(),
        AllowedAttributes = new HashSet<string>(),
        AllowedCssProperties = new HashSet<string>(),
        AllowedSchemes = new HashSet<string>(),
        UriAttributes = HtmlSanitizerDefaults.UriAttributes,
        AllowedAtRules = new HashSet<CssRuleType>(),
        AllowedCssClasses = new HashSet<string>()
    });


    public static bool IsXssInjected(string rawText)
    {
        var sanitised = DefaultSanitizer.Sanitize(rawText);
        return !rawText.Equals(sanitised);
    }

    public static bool IsStrictXssInjected(string rawText)
    {
        var sanitised = StrictSanitizer.Sanitize(rawText);
        return !rawText.Equals(sanitised);
    }

    public static async Task<bool> IsXssInjected(HttpContext httpContext)
    {
        var raw = await GetStringsFromHttpContext(httpContext);
        return IsXssInjected(raw);
    }

    public static async Task<bool> IsStrictXssInjected(HttpContext httpContext)
    {
        var raw = await GetStringsFromHttpContext(httpContext);
        return IsStrictXssInjected(raw);
    }

    private static async Task<string> GetStringsFromHttpContext(HttpContext httpContext)
    {
        // enable buffering so that the request can be read by the model binders next
        httpContext.Request.EnableBuffering();

        // leaveOpen: true to leave the stream open after disposing,
        // so it can be read by the model binders
        using var streamReader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true);

        var raw = await streamReader.ReadToEndAsync();
        // rewind the stream for the next middleware
        httpContext.Request.Body.Seek(0, SeekOrigin.Begin);

        return raw;
    }
}