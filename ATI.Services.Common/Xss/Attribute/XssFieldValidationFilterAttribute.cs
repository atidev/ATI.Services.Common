using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ATI.Services.Common.Xss.Attribute;

public class XssFieldValidationFilterAttribute : System.Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await XssAttributesHelper.FieldValidationAction(context, next, XssHelper.IsXssInjected);
    }
}