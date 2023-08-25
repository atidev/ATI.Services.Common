using ATI.Services.Common.Xss.Attribute;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Common.Xss;

public static class XssExtensions
{
    public static IApplicationBuilder UseXssValidation(this IApplicationBuilder builder)
    { 
      return builder.UseMiddleware<XssMiddleware>();
    }
    
    public static IMvcBuilder AddXssValidationAttribute(this IServiceCollection services)
    {
      return services.AddControllers(options =>
      {
        options.Filters.Add(typeof(XssFieldValidationFilterAttribute));
      });
    }
}
