using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ATI.Services.Common.Behaviors.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
#nullable enable

namespace ATI.Services.Common.Behaviors;

public static class CommonBehavior
{
    [PublicAPI]
    public const string AllowAllOriginsCorsPolicyName = "AllowAllOrigins";
    private const string InternalServerErrorMessage = "internal_error";
    public const string ServiceNameItemKey = "ServiceName";
    public const string ClientNameItemKey = "ClientName";

    private static Dictionary<ActionStatus, ExtendedErrorResponse>? CustomResponses { get; set; }

    private static readonly Array EmptyArray = Array.Empty<object>();

    /// <summary>
    /// Устанавливает параметры сериализации. 
    /// </summary>
    /// <param name="jsonSerializerSettings"></param>
    [PublicAPI]
    public static void SetSerializer(JsonSerializerSettings jsonSerializerSettings)
    {
        JsonSerializerSettings = jsonSerializerSettings;
    }

    [PublicAPI]
    public static void SetCustomResponses(Dictionary<ActionStatus, ExtendedErrorResponse> customStatusCodes)
    {
        CustomResponses = customStatusCodes;
    }

    internal static JsonSerializerSettings JsonSerializerSettings =
        new()
        {
            DefaultValueHandling = DefaultValueHandling.Include,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy
                {
                    ProcessDictionaryKeys = true,
                    OverrideSpecifiedNames = true
                }
            }
        };

    internal static string GetMessage(
        Func<ActionStatus, string?>? beautifulMessageFunc,
        ActionStatus actionStatus,
        string privateError,
        string? publicError,
        string defaultMessage,
        bool isInternal
    )
    {
        if (isInternal && !string.IsNullOrWhiteSpace(privateError))
        {
            return privateError;
        }

        if (!string.IsNullOrWhiteSpace(publicError))
        {
            return publicError;
        }

        if (beautifulMessageFunc == null)
        {
            return defaultMessage;
        }

        return beautifulMessageFunc(actionStatus) ?? defaultMessage;
    }

    internal static string GetError(ActionStatus actionStatus,
        Func<ActionStatus, string?>? customErrorCodeFunc) =>
        customErrorCodeFunc?.Invoke(actionStatus) ?? GetDefaultError(actionStatus);

    private static string GetDefaultError(ActionStatus status)
    {
        if (CustomResponses != null
            && CustomResponses.TryGetValue(status, out var errorResponse)
            && errorResponse.Error != null)
            return errorResponse.Error;

        return status switch
        {
            ActionStatus.NotFound 
                or ActionStatus.NoContent => "not_found",
            ActionStatus.Forbidden 
                or ActionStatus.ModificationRestricted 
                or ActionStatus.Duplicates
                or ActionStatus.SelfDuplicates 
                or ActionStatus.FullRegistrationRequired => "forbidden",
            ActionStatus.BadRequest 
                or ActionStatus.ConstraintError 
                or ActionStatus.LogicalError =>
                "invalid_input_data",
            ActionStatus.Timeout => "timeout",
            ActionStatus.InternalServerError 
                or ActionStatus.InternalOptionalServerUnavailable
                or ActionStatus.ConfigurationError => InternalServerErrorMessage,
            ActionStatus.UnprocessableEntity => "un_processable_entity",
            ActionStatus.ExternalServerError 
                or ActionStatus.ExternalContractError => "external_service_error",
            ActionStatus.PaymentRequired => "payment_required",
            ActionStatus.Unauthorized => "un_authorized",
            ActionStatus.TooManyRequests => "too_many_requests",
            _ => "unknown_error"
        };
    }
    internal static string GetDefaultMessage(ActionStatus status)
    {
        if (CustomResponses != null
            && CustomResponses.TryGetValue(status, out var errorResponse)
            && errorResponse.Reason != null)
            return errorResponse.Reason;

        return status switch
        {
            ActionStatus.NotFound 
                or ActionStatus.NoContent => ApiMessages.NotFoundErrorMessage,
            ActionStatus.Forbidden 
                or ActionStatus.FullRegistrationRequired => ApiMessages.ForbiddenCommonMessage,
            ActionStatus.ModificationRestricted => ApiMessages.ModificationRestrictedCommonMessage,
            ActionStatus.Duplicates 
                or ActionStatus.SelfDuplicates => ApiMessages.DuplicatesErrorCommonMessage,
            ActionStatus.BadRequest 
                or ActionStatus.ConstraintError => ApiMessages.BadRequestCommonMessage,
            ActionStatus.LogicalError => ApiMessages.LogicalErrorCommonMessage,
            ActionStatus.Timeout => ApiMessages.TimeoutCommonMessage,
            ActionStatus.InternalServerError 
                or ActionStatus.InternalOptionalServerUnavailable
                or ActionStatus.ConfigurationError => ApiMessages.InternalServerError,
            ActionStatus.UnprocessableEntity => ApiMessages.BadRequestCommonMessage,
            ActionStatus.ExternalServerError 
                or ActionStatus.ExternalContractError => ApiMessages.ExternalServiceError,
            ActionStatus.PaymentRequired => ApiMessages.ForbiddenOnlyPaidMessage,
            ActionStatus.Unauthorized => ApiMessages.Unauthorized,
            ActionStatus.TooManyRequests => ApiMessages.TooManyRequests,
            _ => ApiMessages.UnknownErrorMessage
        };
    }

    internal static HttpStatusCode GetStatusCode(ActionStatus actionStatus,
        Func<ActionStatus, HttpStatusCode?>? customStatusFunc = null) =>
        customStatusFunc?.Invoke(actionStatus) ?? GetDefaultStatusCode(actionStatus);

    private static HttpStatusCode GetDefaultStatusCode(ActionStatus actionStatus)
    {
        if (CustomResponses != null
            && CustomResponses.TryGetValue(actionStatus, out var errorResponse))
            return errorResponse.StatusCode;

        return actionStatus switch
        {
            ActionStatus.NotFound 
                or ActionStatus.NoContent => HttpStatusCode.NotFound,
            ActionStatus.Forbidden 
                or ActionStatus.ModificationRestricted 
                or ActionStatus.Duplicates
                or ActionStatus.SelfDuplicates => HttpStatusCode.Forbidden,
            ActionStatus.BadRequest 
                or ActionStatus.ConstraintError 
                or ActionStatus.LogicalError
                or ActionStatus.UnprocessableEntity
                or ActionStatus.FullRegistrationRequired => HttpStatusCode.BadRequest,
            ActionStatus.InternalServerError 
                or ActionStatus.InternalOptionalServerUnavailable => HttpStatusCode.InternalServerError,
            ActionStatus.Timeout => HttpStatusCode.GatewayTimeout,
            ActionStatus.PaymentRequired => HttpStatusCode.PaymentRequired,
            ActionStatus.ExternalContractError or ActionStatus.ExternalServerError => HttpStatusCode.ServiceUnavailable,
            ActionStatus.Unauthorized => HttpStatusCode.Unauthorized,
            ActionStatus.TooManyRequests => HttpStatusCode.TooManyRequests,
            _ => HttpStatusCode.OK
        };
    }

    public static IActionResult GetActionResult(
        ActionStatus status,
        bool isInternal,
        string? reason = null,
        string? error = null,
        string? internalReason = null,
        bool resultIsArray = false,
        Func<ActionStatus, HttpStatusCode?>? customStatusFunc = null,
        Func<ActionStatus, string>? customErrorCodeFunc = null,
        Func<ActionStatus, string>? customMessageFunc = null)
    {
        return status switch
        {
            ActionStatus.Ok => OkResult,
            ActionStatus.NotFound
                or ActionStatus.NoContent when resultIsArray => new JsonResult(EmptyArray),
            _ => new JsonResult(new FullErrorResponse
                {
                    Error = error ?? GetError(status, customErrorCodeFunc),
                    Reason = isInternal && !string.IsNullOrEmpty(internalReason)
                        ? internalReason
                        : reason ?? customMessageFunc?.Invoke(status) ?? GetDefaultMessage(status)
                })
                { StatusCode = (int)GetStatusCode(status, customStatusFunc) }
        };
    }

    [PublicAPI]
    public static IActionResult GetActionResult(ActionStatus status,
        ModelStateDictionary modelState,
        bool resultIsArray = false,
        Func<ActionStatus, HttpStatusCode?>? customStatusFunc = null,
        Func<ActionStatus, string>? customErrorCodeFunc = null,
        bool useModelStateKeysAsErrors = false)
    {
        return status switch
        {
            ActionStatus.Ok => OkResult,
            ActionStatus.NotFound
                or ActionStatus.NoContent when resultIsArray => new JsonResult(EmptyArray),
            _ when modelState.GetErrors().Distinct().Count() > 1 =>
                new JsonResult(new FullErrorResponse
                {
                    Error = GetError(status, customErrorCodeFunc),
                    Reason = GetDefaultMessage(status),
                    ErrorList = useModelStateKeysAsErrors 
                        ? modelState
                            .Where(pair => pair.Value?.Errors.Count > 0)
                            .Select(pair => new ErrorResponse
                            {
                                Error = pair.Key,
                                Reason = string.Join(".\n ", (pair.Value?.Errors ?? []).Select(error => error.ErrorMessage).Distinct())
                            }).ToList()
                        : modelState.GetErrors().Select(
                            error => new ErrorResponse
                            {
                                Error = GetError(status, customErrorCodeFunc),
                                Reason = error
                            }).ToList()
                })
                {
                    StatusCode = (int)GetStatusCode(status, customStatusFunc)
                },
            _ => new JsonResult(new FullErrorResponse
                {
                    Error = useModelStateKeysAsErrors 
                        ? modelState.FirstOrDefault(pair => pair.Value?.Errors.Count > 0).Key
                        : GetError(status, customErrorCodeFunc),
                    Reason = !string.IsNullOrEmpty(modelState.GetErrors().FirstOrDefault())
                        ? modelState.GetErrors().FirstOrDefault()
                        : GetDefaultMessage(status)
                })
                { StatusCode = (int)GetStatusCode(status, customStatusFunc) }
        };
    }

    private static readonly IActionResult OkResult = new OkResult();
    private static readonly IActionResult OkResultWithEmptyArray = new JsonResult(EmptyArray);
        
    internal static IActionResult GetActionResult<T>(ActionStatus status, 
        bool isInternal, 
        string? reason = null,
        Func<ActionStatus, HttpStatusCode?>? customStatusFunc = null,
        Func<ActionStatus, string>? customErrorCodeFunc = null)
    {
        return status switch
        {
            ActionStatus.Ok => OkResult,
            ActionStatus.NotFound
                or ActionStatus.NoContent
                when typeof(IEnumerable).IsAssignableFrom(typeof(T))
                     && !typeof(IDictionary).IsAssignableFrom(typeof(T)) => OkResultWithEmptyArray,
            _ => new JsonResult(new FullErrorResponse
                {
                    Error = GetError(status, customErrorCodeFunc),
                    Reason = isInternal && !string.IsNullOrEmpty(reason) ? reason : GetDefaultMessage(status)
                })
                { StatusCode = (int)GetStatusCode(status, customStatusFunc) }
        };
    }
}