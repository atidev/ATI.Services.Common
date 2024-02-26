using System;
using System.Collections.Generic;
using System.Linq;
using ATI.Services.Common.Context;
using ATI.Services.Common.Variables;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Common.Localization;

[PublicAPI]
public static class LocalizationExtensions
{
    public static IServiceCollection AddInCodeLocalization(this IServiceCollection services)
    {
            foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                                          .SelectMany(s => s.GetTypes())
                                          .Where(p => p.IsClass && p.IsAssignableTo(typeof(IInCodeLocalization))))
            {
                services.AddSingleton(typeof(IInCodeLocalization), type);
            }
        
            services.AddSingleton<InCodeLocalizer>();
            return services;
        }

        
    public static void UseAcceptLanguageLocalization(this IApplicationBuilder builder,
        List<IRequestCultureProvider> additionalProviders = null)
    {
            var requestLocalizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture(ServiceVariables.DefaultLocale)
                .AddSupportedUICultures(ServiceVariables.SupportedLocales.ToArray());
            requestLocalizationOptions.FallBackToParentUICultures = false;

            var providers = new List<IRequestCultureProvider>
            {
                new AcceptLanguageHeaderRequestCultureProvider
                {
                    MaximumAcceptLanguageHeaderValuesToTry = 10
                }
            };
            
            if (additionalProviders != null)
                providers.AddRange(additionalProviders);

            requestLocalizationOptions.RequestCultureProviders = providers;

            builder.UseRequestLocalization(requestLocalizationOptions);

            builder.Use(async (context, next) =>
            {
                var acceptLanguageHeader = context.Request.Headers.AcceptLanguage;
                FlowContext<RequestMetaData>.Current = new RequestMetaData
                    { RequestAcceptLanguage = acceptLanguageHeader };

                await next(context);
            });
        }
}