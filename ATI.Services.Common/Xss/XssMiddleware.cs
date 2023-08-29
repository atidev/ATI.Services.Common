using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ATI.Services.Common.Xss;

public class XssMiddleware
{
    private readonly RequestDelegate _next;

    public XssMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (await XssHelper.IsXssInjected(httpContext))
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await httpContext.Response.WriteAsync("XSS injection detected from middleware.");
        }
        else
        {
            await _next(httpContext);
        }
    }
}