using ATI.Services.Common.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Common.Mattermost;

[PublicAPI]
public static class MattermostExtensions
{
    public static IServiceCollection AddMattermost(this IServiceCollection services)
    {
        services.ConfigureByName<MattermostProviderOptions>();

        services.AddHttpClient(MattermostAdapter.HttpClientName);
        
        return services.AddSingleton<MattermostProvider>();
    }
}