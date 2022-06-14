using System.Collections.Generic;
using System.Globalization;
using ATI.Services.Common.Context;
using ATI.Services.Common.Variables;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;

namespace ATI.Services.Common.Localization
{
    [PublicAPI]
    public static class LocalizationExtensions
    {
        public static void UseAcceptLanguageLocalization(this IApplicationBuilder builder,
            List<IRequestCultureProvider> additionalProviders = null)
        {
            var requestLocalizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture(ServiceVariables.DefaultLocale)
                .AddSupportedCultures(ServiceVariables.SupportedLocales)
                .AddSupportedUICultures(ServiceVariables.SupportedLocales);

            var providers = new List<IRequestCultureProvider>
            {
                new AcceptLanguageHeaderRequestCultureProvider()
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
}