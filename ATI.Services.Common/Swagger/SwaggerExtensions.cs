using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ATI.Services.Common.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;
using ConfigurationManager = ATI.Services.Common.Behaviors.ConfigurationManager;

namespace ATI.Services.Common.Swagger
{
    [PublicAPI]
    public static class SwaggerExtensions
    {
        /// <summary>
        /// Добавляет Swagger
        /// </summary>
        /// <param name="services"></param>
        /// <param name="additionalOptions">Дополнительные параметры</param>
        public static void AddAtiSwagger(this IServiceCollection services, Action<SwaggerGenOptions> additionalOptions = null)
        {
            services.ConfigureByName<SwaggerOptions>();
            
            var swaggerOptions = ConfigurationManager.GetSection("SwaggerOptions").Get<SwaggerOptions>();

            if (!swaggerOptions.Enabled) return;
            
            services.AddSwaggerGen(c =>
            {
                foreach (var tag in Enum.GetNames(typeof(SwaggerTag)))
                {
                    c.SwaggerDoc(tag, new OpenApiInfo { Title = $"{swaggerOptions.ServiceName} {tag} API", 
                        Version = swaggerOptions.Version });
                }

                foreach (var securityHeader in swaggerOptions.SecurityApiKeyHeaders)
                {
                    c.AddSecurityDefinition(securityHeader,
                        new OpenApiSecurityScheme { In = ParameterLocation.Header,
                            Description = $"Please enter {securityHeader}", 
                            Name = securityHeader, Type = SecuritySchemeType.ApiKey});

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = securityHeader
                                },
                                Name = securityHeader,
                                In = ParameterLocation.Header

                            },
                            new List<string>()
                        }
                    });
                }

                if (swaggerOptions.ProjectsXmlNames == null)
                {
                    foreach (string projectXmlPath in Directory.EnumerateFiles(AppContext.BaseDirectory, "*.xml"))
                    {
                        c.IncludeXmlComments(projectXmlPath);
                    }
                }
                else
                {
                    foreach (var projectXml in swaggerOptions.ProjectsXmlNames)
                    {
                        c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, projectXml));
                    }
                }
                    
                c.OrderActionsBy(apiDesc => $"{apiDesc.HttpMethod}");
                c.DocInclusionPredicate((docName, apiDesc) =>
                {
                    if (!apiDesc.TryGetMethodInfo(out MethodInfo methodInfo)) return false;

                    var tags =
                        methodInfo.GetCustomAttributes(true)
                            // ReSharper disable once PossibleNullReferenceException
                            .Union(methodInfo.DeclaringType.GetCustomAttributes(true))
                            .OfType<SwaggerTagAttribute>()
                            .Select(attr => attr.Tag).Aggregate((SwaggerTag)0, (result, next) => result | next);

                    return Enum.TryParse(docName, out SwaggerTag docTag) && tags.HasFlag(docTag);
                });
                    
                additionalOptions?.Invoke(c);
            });
            services.AddSwaggerGenNewtonsoftSupport();
        }
        
        public static void UseAtiSwagger(this IApplicationBuilder app,
            Action<Swashbuckle.AspNetCore.Swagger.SwaggerOptions> customSwaggerOptions = null,
            Action<SwaggerUIOptions> customUiOptions = null)
        {
            var swaggerOptions = app.ApplicationServices.GetRequiredService<IOptions<SwaggerOptions>>().Value;
            if (!swaggerOptions.Enabled) return;
            
            app.UseSwagger(c =>
            {
                customSwaggerOptions?.Invoke(c);
            });
            app.UseSwaggerUI(c =>
            {
                c.EnableDeepLinking();
                c.DisplayOperationId();
                foreach (var tag in Enum.GetNames(typeof(SwaggerTag)))
                {
                    c.SwaggerEndpoint($"/swagger/{tag}/swagger.json", $"{swaggerOptions.ServiceName} {tag} API");
                }
                c.ShowExtensions();
                customUiOptions?.Invoke(c);
            });
        }
    }
}