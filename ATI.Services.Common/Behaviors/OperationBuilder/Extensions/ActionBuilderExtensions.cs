using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;

namespace ATI.Services.Common.Behaviors.OperationBuilder.Extensions
{
    [PublicAPI]
    public static class ActionBuilderExtensions
    {
        public static ActionBuilder AsActionBuilder(this Task<OperationResult> operation)
        {
            return new(operation);
        }

        public static ActionBuilder AsActionBuilder(this OperationResult operation)
        {
            return new(operation);
        }

        public static async Task<IActionResult> AsActionResultAsync(this Task<OperationResult> operationTask,
            bool? internalFlag = null)
        {
            var actionBuilder = new ActionBuilder(operationTask);
            if (internalFlag.HasValue)
            {
                actionBuilder.WithInternalFlag(internalFlag.Value);
            }

            return await new ActionBuilder(operationTask).ExecuteAsync();
        }

        public static IActionResult AsActionResult(this OperationResult operation, bool? internalFlag = null)
        {
            var actionBuilder = new ActionBuilder(operation);
            if (internalFlag.HasValue)
            {
                actionBuilder.WithInternalFlag(internalFlag.Value);
            }

            return actionBuilder.Execute();
        }

        public static ActionBuilder WithActionResultRewrite(this ActionBuilder builder,
            Dictionary<ActionStatus, IActionResult> resultRewrites)
        {
            builder.ActionResultRewrite = resultRewrites;
            return builder;
        }

        public static T WithInternalFlag<T>(this T action, bool isInternal) where T : BaseActionBuilder
        {
            action.IsInternal = isInternal;
            return action;
        }

        public static T WithCustomStatus<T>(this T action, Func<ActionStatus, HttpStatusCode?> customStatusFunc)
            where T : BaseActionBuilder
        {
            action.GetCustomStatus = customStatusFunc;
            return action;
        }

        public static T WithCustomStatus<T>(this T action, IDictionary<ActionStatus, HttpStatusCode?> customStatusMap)
            where T : BaseActionBuilder
        {
            action.GetCustomStatus = status =>
                customStatusMap.TryGetValue(status, out var statusCode) ? statusCode : null;
            ;
            return action;
        }

        public static T WithCustomErrorCode<T>(this T action, Func<ActionStatus, string> customErrorCodeFunc)
            where T : BaseActionBuilder
        {
            action.GetCustomErrorCode = customErrorCodeFunc;
            return action;
        }

        public static T WithCustomErrorCode<T>(this T action, IDictionary<ActionStatus, string> customErrorCodeMap)
            where T : BaseActionBuilder
        {
            action.GetCustomErrorCode =
                status => customErrorCodeMap.TryGetValue(status, out var message) ? message : null;
            return action;
        }

        public static T WithCustomMessage<T>(this T action, Func<ActionStatus, string> customMessageFunc)
            where T : BaseActionBuilder
        {
            action.GetCustomMessage = customMessageFunc;
            return action;
        }

        public static T WithCustomMessage<T>(this T action, IDictionary<ActionStatus, string> customMessageMap)
            where T : BaseActionBuilder
        {
            action.GetCustomMessage = status => customMessageMap.TryGetValue(status, out var message) ? message : null;
            return action;
        }

        public static TFunc WithNotFoundCondition<TFunc, T>(this TFunc function, Func<T, bool> notFoundCondition)
            where TFunc : BaseFunctionBuilder<T>
        {
            function.NotFoundCondition = notFoundCondition;
            return function;
        }
    }
}