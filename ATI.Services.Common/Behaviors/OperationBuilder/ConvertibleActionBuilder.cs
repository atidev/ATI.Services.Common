using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ATI.Services.Common.Behaviors.OperationBuilder
{
    public class ConvertibleActionBuilder<TIn, TRes> : BaseFunctionBuilder<TIn>
    {
        private Func<TIn, TRes> ConvertFunc { get; }
        protected internal Func<TRes, bool> NotFoundConditionAfterDataConvert { get; set; }
        protected internal bool NullNotFoundConditionAfterDataConvert { get; set; }
        protected internal Dictionary<ActionStatus, Func<TRes, IActionResult>> ActionResultRewrite { get; set; }

        public ConvertibleActionBuilder(Task<OperationResult<TIn>> functionTask, Func<TIn, TRes> convertFunc)
        {
            FunctionTask = functionTask;
            ConvertFunc = convertFunc;
        }
        public ConvertibleActionBuilder(OperationResult<TIn> functionResult, Func<TIn, TRes> convertFunc)
        {
            FunctionResult = functionResult;
            ConvertFunc = convertFunc;
        }

        public async Task<IActionResult> ExecuteAsync(JsonSerializerSettings jsonSettings = null)
        {
            var operationResult = FunctionTask != null ? await FunctionTask : FunctionResult;
            return ExecutePrivateAsync(operationResult, jsonSettings);
        }

        public IActionResult Execute(JsonSerializerSettings jsonSettings = null)
        {
            var operationResult = FunctionResult ?? FunctionTask.GetAwaiter().GetResult();
            return ExecutePrivateAsync(operationResult, jsonSettings);
        }

        private IActionResult ExecutePrivateAsync(OperationResult<TIn> operationResult, JsonSerializerSettings jsonSettings = null)
        {
            try
            {
                if (ActionResultRewrite != null)
                {
                    Func<TRes, IActionResult> resultFunc;
                    if (NullNotFoundCondition && operationResult.Value == null)
                    {
                        if (ActionResultRewrite.TryGetValue(ActionStatus.NotFound, out resultFunc))
                        {
                            return resultFunc(default);
                        }
                    }
                    if (NotFoundCondition != null && NotFoundCondition(operationResult.Value))
                    {
                        if (ActionResultRewrite.TryGetValue(ActionStatus.NotFound, out resultFunc))
                        {
                            return resultFunc(default);
                        }
                    }

                    if (ActionResultRewrite.TryGetValue(operationResult.ActionStatus, out resultFunc))
                    {
                        return resultFunc(ConvertFunc(operationResult.Value));
                    }
                }

                IActionResult result = null;
                if (!CheckActionStatus(
                    ref result,
                    operationResult,
                    IsInternal,
                    GetCustomMessage,
                    GetCustomStatus,
                    GetCustomErrorCode))
                {
                    return result;
                }

                if (operationResult.ActionStatus == ActionStatus.NotFound ||
                    NullNotFoundCondition && operationResult.Value == null ||
                    NotFoundCondition != null && NotFoundCondition(operationResult.Value))
                {
                    return CommonBehavior.GetActionResult<TRes>(ActionStatus.NotFound, IsInternal,
                        customStatusFunc: GetCustomStatus);
                }

                var convertedData = ConvertFunc(operationResult.Value);

                if (NullNotFoundConditionAfterDataConvert && convertedData == null
                    || NotFoundConditionAfterDataConvert != null && NotFoundConditionAfterDataConvert(convertedData))
                {
                    return CommonBehavior.GetActionResult<TRes>(ActionStatus.NotFound, IsInternal,
                        customStatusFunc: GetCustomStatus);
                }

                if (ShouldSerializePrivateProperties.HasValue)
                {
                    ((IPrivatePropertiesContainer)convertedData)?.SetShouldSerializePrivateProperties(
                        ShouldSerializePrivateProperties.Value);
                }

                return new JsonResult(convertedData, jsonSettings ?? CommonBehavior.JsonSerializerSettings) { StatusCode = (int)HttpStatusCode.OK };

            }
            catch (Exception e)
            {
                Logger.Error(e);
                return CommonBehavior.GetActionResult(ActionStatus.InternalServerError, IsInternal, e.Message);
            }
        }
    }
}