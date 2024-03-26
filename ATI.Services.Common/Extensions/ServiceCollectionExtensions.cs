﻿using System;
using ATI.Services.Common.Initializers;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ConfigurationManager = ATI.Services.Common.Behaviors.ConfigurationManager;

namespace ATI.Services.Common.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void ConfigureByName<T>(this IServiceCollection serviceCollection, bool checkSectionExists = true)
            where T : class
        {
            var sectionName = typeof(T).Name;
            
            var section = ConfigurationManager.GetSection(sectionName);
            // Если секции не существует - что-то забыли. Бросаем эксепшн при старте приложения
            if(checkSectionExists && !section.Exists())
                throw new Exception($"Секции {sectionName} нет в appsettings.json");
            
            serviceCollection.Configure<T>(section);
        }

        [UsedImplicitly]
        public static IServiceCollection AddInitializers(this IServiceCollection services)
            => services.AddTransient<StartupInitializer>();
    }
}
