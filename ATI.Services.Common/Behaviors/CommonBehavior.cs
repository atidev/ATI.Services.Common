using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using ATI.Services.Common.Extensions;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ATI.Services.Common.Behaviors;

public static class CommonBehavior
{
    [PublicAPI]
    public const string AllowAllOriginsCorsPolicyName = "AllowAllOrigins";
    private const string InternalServerErrorMessage = "internal_error";
    public const string ServiceNameItemKey = "ServiceName";
    public const string ClientNameItemKey = "ClientName";

    private static Dictionary<ActionStatus, ExtendedErrorResponse> CustomResponses { get; set; }

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
        Func<ActionStatus, string> beautifulMessageFunc,
        ActionStatus actionStatus,
        string privateError,
        string publicError,
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
        Func<ActionStatus, string> customErrorCodeFunc) =>
        customErrorCodeFunc?.Invoke(actionStatus) ?? GetDefaultError(actionStatus);

    private static string GetDefaultError(ActionStatus status)
    {
            if (CustomResponses != null
                && CustomResponses.TryGetValue(status, out var errorResponse)
                && errorResponse.Error != null)
                return errorResponse.Error;

            switch (status)
            {
                case ActionStatus.NotFound:
                case ActionStatus.NoContent:
                    return "not_found";
                case ActionStatus.Forbidden:
                case ActionStatus.ModificationRestricted:
                case ActionStatus.Duplicates:
                case ActionStatus.SelfDuplicates:
                case ActionStatus.FullRegistrationRequired:
                    return "forbidden";
                case ActionStatus.BadRequest:
                case ActionStatus.ConstraintError:
                case ActionStatus.LogicalError:
                    return "invalid_input_data";
                case ActionStatus.Timeout:
                    return "timeout";
                case ActionStatus.InternalServerError:
                case ActionStatus.InternalOptionalServerUnavailable:
                case ActionStatus.ConfigurationError:
                    return InternalServerErrorMessage;
                case ActionStatus.UnprocessableEntity:
                    return "un_processable_entity";
                case ActionStatus.ExternalServerError:
                case ActionStatus.ExternalContractError:
                    return "external_service_error";
                case ActionStatus.PaymentRequired:
                    return "payment_required";
                case ActionStatus.Unauthorized:
                    return "un_authorized";
                case ActionStatus.TooManyRequests:
                    return "too_many_requests";
            }

            return "unknown_error";
        }
    internal static string GetDefaultMessage(ActionStatus status)
    {
            if (CustomResponses != null
                && CustomResponses.TryGetValue(status, out var errorResponse)
                && errorResponse.Reason != null)
                return errorResponse.Reason;

            switch (status)
            {
                case ActionStatus.NotFound:
                case ActionStatus.NoContent:
                    return ApiMessages.NotFoundErrorMessage;
                case ActionStatus.Forbidden:
                case ActionStatus.FullRegistrationRequired:
                    return ApiMessages.ForbiddenCommonMessage;
                case ActionStatus.ModificationRestricted:
                    return ApiMessages.ModificationRestrictedCommonMessage;
                case ActionStatus.Duplicates:
                case ActionStatus.SelfDuplicates:
                    return ApiMessages.DuplicatesErrorCommonMessage;
                case ActionStatus.BadRequest:
                case ActionStatus.ConstraintError:
                    return ApiMessages.BadRequestCommonMessage;
                case ActionStatus.LogicalError:
                    return ApiMessages.LogicalErrorCommonMessage;
                case ActionStatus.Timeout:
                    return ApiMessages.TimeoutCommonMessage;
                case ActionStatus.InternalServerError:
                case ActionStatus.InternalOptionalServerUnavailable:
                case ActionStatus.ConfigurationError:
                    return ApiMessages.InternalServerError;
                case ActionStatus.UnprocessableEntity:
                    return ApiMessages.BadRequestCommonMessage;
                case ActionStatus.ExternalServerError:
                case ActionStatus.ExternalContractError:
                    return ApiMessages.ExternalServiceError;
                case ActionStatus.PaymentRequired:
                    return ApiMessages.ForbiddenOnlyPaidMessage;
                case ActionStatus.Unauthorized:
                    return ApiMessages.Unauthorized;
                case ActionStatus.TooManyRequests:
                    return ApiMessages.TooManyRequests;
            }

            return ApiMessages.UnknownErrorMessage;
        }

    internal static HttpStatusCode GetStatusCode(ActionStatus actionStatus,
        Func<ActionStatus, HttpStatusCode?> customStatusFunc = null) =>
        customStatusFunc?.Invoke(actionStatus) ?? GetDefaultStatusCode(actionStatus);

    private static HttpStatusCode GetDefaultStatusCode(ActionStatus actionStatus)
    {
            if (CustomResponses != null
                && CustomResponses.TryGetValue(actionStatus, out var errorResponse))
                return errorResponse.StatusCode;

            switch (actionStatus)
            {
                case ActionStatus.NotFound:
                case ActionStatus.NoContent:
                    return HttpStatusCode.NotFound;
                case ActionStatus.Forbidden:
                case ActionStatus.ModificationRestricted:
                case ActionStatus.Duplicates:
                case ActionStatus.SelfDuplicates:
                    return HttpStatusCode.Forbidden;
                case ActionStatus.BadRequest:
                case ActionStatus.ConstraintError:
                case ActionStatus.LogicalError:
                case ActionStatus.UnprocessableEntity:
                case ActionStatus.FullRegistrationRequired:
                    return HttpStatusCode.BadRequest;
                case ActionStatus.InternalServerError:
                case ActionStatus.InternalOptionalServerUnavailable:
                    return HttpStatusCode.InternalServerError;
                case ActionStatus.Timeout:
                    return HttpStatusCode.GatewayTimeout;
                case ActionStatus.PaymentRequired:
                    return HttpStatusCode.PaymentRequired;
                case ActionStatus.ExternalContractError:
                case ActionStatus.ExternalServerError:
                    return HttpStatusCode.ServiceUnavailable;
                case ActionStatus.Unauthorized:
                    return HttpStatusCode.Unauthorized;
                case ActionStatus.TooManyRequests:
                    return HttpStatusCode.TooManyRequests;
                default:
                    return HttpStatusCode.OK;
            }
        }

    public static IActionResult GetActionResult(
        ActionStatus status,
        bool isInternal,
        string reason = null,
        string error = null,
        string internalReason = null,
        bool resultIsArray = false,
        Func<ActionStatus, HttpStatusCode?> customStatusFunc = null,
        Func<ActionStatus, string> customErrorCodeFunc = null,
        Func<ActionStatus, string> customMessageFunc = null)
    {
            switch (status)
            {
                case ActionStatus.Ok:
                    return new OkResult();
                case ActionStatus.NotFound:
                case ActionStatus.NoContent:
                    if (resultIsArray)
                        return new JsonResult(EmptyArray);
                    break;
            }

            return new JsonResult(new FullErrorResponse
            {
                Error = error ?? GetError(status, customErrorCodeFunc),
                Reason = isInternal && !string.IsNullOrEmpty(internalReason)
                        ? internalReason
                        : reason ?? customMessageFunc?.Invoke(status) ?? GetDefaultMessage(status)
            })
            { StatusCode = (int)GetStatusCode(status, customStatusFunc) };
        }

    [PublicAPI]
    public static IActionResult GetActionResult(ActionStatus status,
        ModelStateDictionary modelState,
        bool resultIsArray = false,
        Func<ActionStatus, HttpStatusCode?> customStatusFunc = null,
        Func<ActionStatus, string> customErrorCodeFunc = null,
        bool useModelStateKeysAsErrors = false)
    {
            switch (status)
            {
                case ActionStatus.Ok:
                    return new OkResult();
                case ActionStatus.NotFound:
                case ActionStatus.NoContent:
                    if (resultIsArray)
                        return new JsonResult(EmptyArray);
                    break;
            }

            var responseError = GetError(status, customErrorCodeFunc);

            if (modelState.GetErrors().Distinct().Count() > 1)
            {
                return new JsonResult(new FullErrorResponse
                {
                    Error = responseError,
                    Reason = GetDefaultMessage(status),
                    ErrorList = useModelStateKeysAsErrors 
                        ? modelState
                            .Where(pair => pair.Value.Errors.Count > 0)
                            .Select(pair => new ErrorResponse
                            {
                                Error = pair.Key,
                                Reason = string.Join(".\n ", pair.Value.Errors.Select(error => error.ErrorMessage ?? error.Exception?.Message).Distinct())
                            }).ToList()
                        : modelState.GetErrors().Select(
                            error => new ErrorResponse
                            {
                                Error = responseError,
                                Reason = error
                            }).ToList()
                })
                {
                    StatusCode = (int)GetStatusCode(status, customStatusFunc)
                };
            }

            var singleError = modelState.GetErrors()?.FirstOrDefault();
            var singleErrorKey = modelState.FirstOrDefault(pair => pair.Value.Errors.Count > 0).Key;
                
            return new JsonResult(new FullErrorResponse
            {
                Error = useModelStateKeysAsErrors 
                        ? singleErrorKey
                        : GetError(status, customErrorCodeFunc),
                Reason = !string.IsNullOrEmpty(singleError)
                        ? singleError
                        : GetDefaultMessage(status)
            })
            { StatusCode = (int)GetStatusCode(status, customStatusFunc) };
        }

    private static readonly IActionResult OkResult = new OkResult();
    private static readonly IActionResult OkResultWithEmptyArray = new JsonResult(EmptyArray);
        
    internal static IActionResult GetActionResult<T>(ActionStatus status, bool isInternal, string reason = null,
        Func<ActionStatus, HttpStatusCode?> customStatusFunc = null,
        Func<ActionStatus, string> customErrorCodeFunc = null)
    {
            switch (status)
            {
                case ActionStatus.Ok:
                    return OkResult;
                case ActionStatus.NotFound:
                case ActionStatus.NoContent:
                    var tType = typeof(T);
                    if (typeof(IEnumerable).IsAssignableFrom(tType) && !typeof(IDictionary).IsAssignableFrom(tType))
                        return OkResultWithEmptyArray;
                    break;
            }

            return new JsonResult(new FullErrorResponse
            {
                Error = GetError(status, customErrorCodeFunc),
                Reason = isInternal && !string.IsNullOrEmpty(reason) ? reason : GetDefaultMessage(status)
            })
            { StatusCode = (int)GetStatusCode(status, customStatusFunc) };
        }
}