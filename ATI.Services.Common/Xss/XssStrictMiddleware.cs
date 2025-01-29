using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ATI.Services.Common.Xss;

public class XssStrictMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext httpContext)
    {
        if (await XssHelper.IsStrictXssInjected(httpContext))
        {
            httpContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await httpContext.Response.WriteAsync("XSS injection detected from middleware.");
        }
        else
        {
            await next(httpContext);
        }
    }
}