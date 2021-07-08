using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Newtonsoft.Json;
using NLog;

namespace ATI.Services.Common.Logging
{
    [PublicAPI]
    public static class GlobalLoggingExtensions
    {
        public static void UseCustomExceptionHandler(this IApplicationBuilder app, WarningError errorAsWarning = 0)
        {
            app.UseExceptionHandler(new ExceptionHandlerOptions
            {
                ExceptionHandler = async context =>
                {
                    var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                    if (exception != null)
                    {
                        var logContext = new
                        {
                            RequestPath = context.Request.Path,
                            RequestQueryParams = context.Request.QueryString,
                            RemoteHost = context.Connection.RemoteIpAddress?.ToString()
                        };

                        if (errorAsWarning != 0 &&
                            (errorAsWarning.HasFlag(WarningError.BadRequestException) &&
                             exception is BadHttpRequestException ||
                             errorAsWarning.HasFlag(WarningError.ConnectionResetException) &&
                             exception is ConnectionResetException))
                        {
                            LogManager.GetCurrentClassLogger().WarnWithObject(
                                exception.Message,
                                logContext
                            );
                        }
                        else
                        {
                            LogManager.GetCurrentClassLogger().ErrorWithObject(
                                exception,
                                "UNHANDLED EXCEPTION",
                                logContext
                            );
                        }
                    }


                    var response = context.Response;
                    response.ContentType = "application/json; charset=utf-8";
                    await response.WriteAsync(JsonConvert.SerializeObject(new ErrorResponse
                        {Error = "internal_error", Reason = ApiMessages.InternalServerError}));
                }
            });
        }
    }
}