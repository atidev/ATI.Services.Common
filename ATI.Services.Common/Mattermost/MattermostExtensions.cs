using ATI.Services.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.Common.Mattermost;

public static class MattermostExtensions
{
    public static IServiceCollection AddMattermost(this IServiceCollection services)
    {
        services.ConfigureByName<MattermostProviderOptions>();

        services.AddHttpClient(MattermostAdapter.HttpClientName);
        
        return services
            .AddSingleton<MattermostProvider>();
    }
}