using System;
using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ATI.Services.Common.Xss.Attribute;

static class XssAttributesHelper
{
    public static async Task FieldValidationAction(ActionExecutingContext context, ActionExecutionDelegate next,
        Func<string, bool> isXssInjectedFunc)
    {
        var actionArguments = context.ActionArguments;

        foreach (var actionArgumentPair in actionArguments)
        {
            var model = actionArgumentPair.Value;
            var modelType = model.GetType();
            var properties = modelType.GetProperties()
                .Where(x => x.IsDefined(typeof(XssValidateAttribute), true));

            foreach (var property in properties)
            {
                var value = property.GetValue(model)?.ToString();
                if (value == null) continue;

                if (isXssInjectedFunc(value))
                {
                    context.Result = CommonBehavior.GetActionResult(ActionStatus.BadRequest,
                        false, "XSS injection detected from property attribute.");
                    return;
                }
            }
        }

        await next();
    }
}