using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ATI.Services.Common.Behaviors.OperationBuilder
{
    public class GenericActionBuilder<T> : BaseFunctionBuilder<T>
    {
        protected internal Dictionary<ActionStatus, Func<T, IActionResult>> ActionResultRewrite { get; set; }

        public GenericActionBuilder(Task<OperationResult<T>> functionTask)
        {
            FunctionTask = functionTask;
        }
        public GenericActionBuilder(OperationResult<T> functionResult)
        {
            FunctionResult = functionResult;
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

        private IActionResult ExecutePrivateAsync(OperationResult<T> operationResult, JsonSerializerSettings jsonSettings = null)
        {
            try
            {
                if (ActionResultRewrite != null)
                {
                    if (ActionResultRewrite.TryGetValue(operationResult.ActionStatus, out var resultFunc))
                    {
                        return resultFunc(operationResult.Value);
                    }
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
                            return resultFunc(operationResult.Value);
                        }
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

                if (operationResult.ActionStatus == ActionStatus.NotFound || NullNotFoundCondition && operationResult.Value == null ||
                    NotFoundCondition != null && NotFoundCondition(operationResult.Value))
                {
                    return CommonBehavior.GetActionResult<T>(ActionStatus.NotFound, IsInternal,
                        customStatusFunc: GetCustomStatus);
                }
                var convertedData = operationResult.Value;

                if (ShouldSerializePrivateProperties.HasValue)
                {
                    ((IPrivatePropertiesContainer) convertedData)?.SetShouldSerializePrivateProperties(
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
