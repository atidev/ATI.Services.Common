using System.IO;
using System.Text;
using System.Threading.Tasks;
using Ganss.Xss;
using Microsoft.AspNetCore.Http;

namespace ATI.Services.Common.Xss;

public static class XssHelper
{
    private static readonly HtmlSanitizer Sanitizer = new();
    
    public static bool IsXssInjected(string rawText)
    {
        var sanitised = Sanitizer.Sanitize(rawText);

        return !rawText.Equals(sanitised);
    }
    
    public static async Task<bool> IsXssInjected(HttpContext httpContext)
    {
        // enable buffering so that the request can be read by the model binders next
        httpContext.Request.EnableBuffering();
            
        // leaveOpen: true to leave the stream open after disposing,
        // so it can be read by the model binders
        using var streamReader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
            
        var raw = await streamReader.ReadToEndAsync();
        // rewind the stream for the next middleware
        httpContext.Request.Body.Seek(0, SeekOrigin.Begin);
        
        return IsXssInjected(raw);
    }
}