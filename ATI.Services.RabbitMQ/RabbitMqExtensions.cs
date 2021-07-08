using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Extensions;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace ATI.Services.RabbitMQ
{
    public static class RabbitMqExtensions
    {
        [PublicAPI]
        public static void AddEventBus(this IServiceCollection services, string eventbusSectionName = null)
        {
            if (eventbusSectionName != null)
            {
                services.Configure<EventbusOptions>(ConfigurationManager.GetSection(eventbusSectionName));
            }
            else
            {
                services.ConfigureByName<EventbusOptions>();
            }
            
            services.AddSingleton<EventbusManager>();
        }
    }
}