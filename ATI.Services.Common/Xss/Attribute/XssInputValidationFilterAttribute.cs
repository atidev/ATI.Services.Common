using System;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ATI.Services.Common.Xss.Attribute;

[PublicAPI]
[AttributeUsage(AttributeTargets.Method)]
public class XssInputValidationFilterAttribute : System.Attribute, IAsyncResourceFilter
{
    public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        if (await XssHelper.IsXssInjected(httpContext))
        {
            context.Result = CommonBehavior.GetActionResult(ActionStatus.BadRequest,
                false, "XSS injection detected from middleware.");
        }
        else
        {
            await next();
        }
    }
}
