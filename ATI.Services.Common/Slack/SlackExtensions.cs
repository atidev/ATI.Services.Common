using ATI.Services.Common.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Common.Slack
{
    [PublicAPI]
    public static class SlackExtensions
    {
        public static void AddSlack(this IServiceCollection services)
        {
            services.ConfigureByName<SlackProviderOptions>();
            services.AddSingleton<SlackProvider>();
        }
    }
}