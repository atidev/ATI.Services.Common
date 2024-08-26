using ATI.Services.Common.Xss.Attribute;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Common.Xss;

[PublicAPI]
public static class XssExtensions
{
    public static IApplicationBuilder UseXssValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<XssMiddleware>();
    }

    public static IApplicationBuilder UseXssStrictValidation(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<XssStrictMiddleware>();
    }

    public static IMvcBuilder AddXssStrictValidationAttribute(this IServiceCollection services)
    {
        return services.AddControllers(options =>
        {
            options.Filters.Add(typeof(XssFieldStrictValidationFilterAttribute));
        });
    }

    public static IMvcBuilder AddXssValidationAttribute(this IServiceCollection services)
    {
        return services.AddControllers(
            options => { options.Filters.Add(typeof(XssFieldValidationFilterAttribute)); });
    }
}