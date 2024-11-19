using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using NLog;

namespace ATI.Services.Common.Behaviors.OperationBuilder;

public abstract class BaseActionBuilder
{
    internal bool IsInternal { get; set; }

    protected internal Func<ActionStatus, HttpStatusCode?>? GetCustomStatus { protected get; set; }
    protected internal Func<ActionStatus, string?>? GetCustomErrorCode { protected get; set; }
    protected internal Func<ActionStatus, string?>? GetCustomMessage { protected get; set; }


    protected static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

    internal static bool CheckActionStatus([NotNullWhen(false)] ref IActionResult? result,
        OperationResult operationResult,
        bool isInternal,
        Func<ActionStatus, string?>? customMessageFunc = null,
        Func<ActionStatus, HttpStatusCode?>? customStatusFunc = null,
        Func<ActionStatus, string?>? customErrorCodeFunc = null,
        bool hasAdditionalNotFoundCondition = true)
    {
        if (operationResult.Success ||
            hasAdditionalNotFoundCondition && operationResult.ActionStatus == ActionStatus.NotFound)
            return true;

        var (mainError, errorList) = GetOperationResultResponse(operationResult, isInternal, customMessageFunc,
            customErrorCodeFunc);
        result = GetActionResult(operationResult.ActionStatus, mainError, errorList, operationResult.Details,
            customStatusFunc);
        return operationResult.Success;
    }

    private static (ErrorResponse, List<ErrorResponse>) GetOperationResultResponse(
        OperationResult operationResult, bool isInternal,
        Func<ActionStatus, string?>? beautifulMessageFunc = null,
        Func<ActionStatus, string?>? customErrorCodeFunc = null)
    {
        var mainError = new ErrorResponse
        {
            Error = operationResult.Errors.FirstOrDefault()?.Error ?? CommonBehavior.GetError(operationResult.ActionStatus, customErrorCodeFunc),
            Reason = CommonBehavior.GetMessage(
                beautifulMessageFunc,
                operationResult.ActionStatus,
                operationResult.DumpAllErrors(),
                operationResult.DumpPublicErrors(),
                CommonBehavior.GetDefaultMessage(operationResult.ActionStatus),
                isInternal)
        };
        var errorList = new List<ErrorResponse>(operationResult.Errors.Count);
        if (operationResult.Errors.Count > 1)
        {
            errorList = operationResult.Errors.Select(res => 
                new ErrorResponse
            {
                Error = res.Error ?? CommonBehavior.GetError(res.ActionStatus, customErrorCodeFunc),
                Reason = CommonBehavior.GetMessage(
                    beautifulMessageFunc,
                    res.ActionStatus,
                    res.ErrorMessage,
                    res.IsInternal ? null : res.ErrorMessage,
                    CommonBehavior.GetDefaultMessage(res.ActionStatus),
                    isInternal)
            }).ToList();
        }

        return (mainError, errorList);
    }

    private static IActionResult GetActionResult(ActionStatus status, ErrorResponse mainError,
        List<ErrorResponse> errorList, IReadOnlyDictionary<string, object> details,
        Func<ActionStatus, HttpStatusCode?>? customStatusFunc)
    {
        var errorResponse = new FullErrorResponse
        {
            Error = mainError.Error,
            Reason = mainError.Reason
        };
        if (errorList?.Count > 0)
        {
            errorResponse.ErrorList = errorList;
        }

        if (details?.Count > 0)
        {
            errorResponse.Details = details;
        }

        return new JsonResult(errorResponse)
        {
            StatusCode = (int) CommonBehavior.GetStatusCode(status, customStatusFunc)
        };
    }
}