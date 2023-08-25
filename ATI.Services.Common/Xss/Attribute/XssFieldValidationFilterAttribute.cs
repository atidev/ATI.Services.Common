using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ATI.Services.Common.Xss.Attribute;

public class XssFieldValidationFilterAttribute : System.Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
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
          if(value == null) continue;
          
          if (XssHelper.IsXssInjected(value))
          {
            context.Result = CommonBehavior.GetActionResult(ActionStatus.BadRequest,
              false, "XSS injection detected from property attribute.");
          }
        }
      }
    
      await next();
    }
}
