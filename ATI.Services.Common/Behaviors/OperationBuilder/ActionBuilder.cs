using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;


namespace ATI.Services.Common.Behaviors.OperationBuilder
{
    public class ActionBuilder : BaseActionBuilder
    {
        protected internal Dictionary<ActionStatus, IActionResult> ActionResultRewrite { get; set; }
        private Task<OperationResult> ActionTask { get; }
        private OperationResult ActionResult { get; }

        public ActionBuilder(Task<OperationResult> actionTask)
        {
            ActionTask = actionTask;
        }
        public ActionBuilder(OperationResult actionResult)
        {
            ActionResult = actionResult;
        }

        public async Task<IActionResult> ExecuteAsync()
        {
            var operationResult = ActionTask != null ? await ActionTask : ActionResult;
            return ExecutePrivate(operationResult);
        }

        public IActionResult Execute()
        {
            var operationResult = ActionResult ?? ActionTask.GetAwaiter().GetResult();
            return ExecutePrivate(operationResult);
        }

        private IActionResult ExecutePrivate(OperationResult operationResult)
        {
            try
            {
                if (ActionResultRewrite != null &&
                    ActionResultRewrite.TryGetValue(operationResult.ActionStatus, out var rewriteResult))
                {
                    return rewriteResult;
                }

                IActionResult result = new OkResult();
                CheckActionStatus(
                    ref result,
                    operationResult,
                    IsInternal,
                    GetCustomMessage,
                    GetCustomStatus,
                    GetCustomErrorCode,
                    false);
                return result;
            }
            catch (Exception e)
            {
                Logger.Error(e);
                return CommonBehavior.GetActionResult(ActionStatus.InternalServerError, IsInternal, e.Message);
            }
        }
    }
}
