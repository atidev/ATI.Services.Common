using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;

namespace ATI.Services.Common.Extensions
{
    public static class OperationResultExtensions
    {
        public static OperationResult<TOut> Select<TInternal, TOut>(this OperationResult<TInternal> operationResult, [NotNull] Func<TInternal, TOut> map)
        {
            if (operationResult.Success)
                return new OperationResult<TOut>(map(operationResult.Value));
            return new OperationResult<TOut>(operationResult);
        }
        
        public static OperationResult<TOut> SelectOr<TInternal, TOut>(this OperationResult<TInternal> operationResult, [NotNull] Func<TInternal, TOut> map, [NotNull] TOut defaultValue)
        {
            if (operationResult.Success)
                return new OperationResult<TOut>(map(operationResult.Value));
            return new OperationResult<TOut>(defaultValue);
        }
        
        public static OperationResult<TOut> SelectOrElse<TInternal, TOut>(this OperationResult<TInternal> operationResult, [NotNull] Func<TInternal, TOut> map, [NotNull] Func<ActionStatus, IList<OperationError>, TOut> mapError)
        {
            if (operationResult.Success)
                return new OperationResult<TOut>(map(operationResult.Value));
            return new OperationResult<TOut>(mapError(operationResult.ActionStatus, operationResult.Errors));
        }
        
        public static Task<OperationResult<TOut>> SelectAsync<TInternal, TOut>(this OperationResult<TInternal> operationResult, [NotNull] Func<TInternal, Task<OperationResult<TOut>>> map)
        {
            if (operationResult.Success)
                return map(operationResult.Value);
            return Task.FromResult(new OperationResult<TOut>(operationResult));
        }
        
        public static Task<OperationResult<TOut>> SelectOrAsync<TInternal, TOut>(this OperationResult<TInternal> operationResult, [NotNull] Func<TInternal, Task<OperationResult<TOut>>> map, [NotNull] TOut defaultValue)
        {
            if (operationResult.Success)
                return map(operationResult.Value);
            return Task.FromResult(new OperationResult<TOut>(defaultValue));
        }
        
        public static Task<OperationResult<TOut>> SelectOrElseAsync<TInternal, TOut>(this OperationResult<TInternal> operationResult, [NotNull] Func<TInternal, Task<OperationResult<TOut>>> map, [NotNull] Func<ActionStatus, IList<OperationError>, Task<OperationResult<TOut>>> mapError)
        {
            if (operationResult.Success)
                return map(operationResult.Value);
            return mapError(operationResult.ActionStatus, operationResult.Errors);
        }

        public static OperationResult<TInternal> InspectSuccess<TInternal>(this OperationResult<TInternal> operationResult, [NotNull] Action<TInternal> action)
        {
            if (operationResult.Success)
                action(operationResult.Value);
            return operationResult;
        }
        
        public static OperationResult<TInternal> InspectError<TInternal>(this OperationResult<TInternal> operationResult, [NotNull] Action<ActionStatus, IList<OperationError>> action)
        {
            if (!operationResult.Success)
                action(operationResult.ActionStatus, operationResult.Errors);
            return operationResult;
        }
        
        public static bool IsSuccessWith<TInternal>(this OperationResult<TInternal> operationResult, [NotNull] Func<TInternal, bool> predicate)
        {
            return operationResult.Select(predicate).Value;
        }
    }
}