using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;

namespace ATI.Services.Common.Extensions
{
    /// <summary>
    /// Расширения для OperationResult 'T
    /// </summary>
    public static class OperationResultExtensions
    {
        /// <summary>
        ///  Проецирует значение исходного OperationResult в новую форму TOut с помощью функции map.
        ///  Перемещает ошибку из исходного OperationResult в OperationResult 'TOut
        /// </summary>
        public static OperationResult<TOut> Select<TInternal, TOut>(this OperationResult<TInternal> operationResult, [NotNull] Func<TInternal, TOut> map)
        {
            if (operationResult.Success)
                return new OperationResult<TOut>(map(operationResult.Value));
            return new OperationResult<TOut>(operationResult);
        }
        
        /// <summary>
        /// Проецирует значение OperationResult в новую форму с помощью функции map.
        /// Заменяет ошибку в исходном OperationResult на defaultValue
        /// </summary>
        public static OperationResult<TOut> SelectOr<TInternal, TOut>(this OperationResult<TInternal> operationResult, [NotNull] Func<TInternal, TOut> map, [NotNull] TOut defaultValue)
        {
            if (operationResult.Success)
                return new OperationResult<TOut>(map(operationResult.Value));
            return new OperationResult<TOut>(defaultValue);
        }
        
        /// <summary>
        /// Проецирует значение OperationResult в новую форму с помощью функции map.
        /// Проецирует ошибку из OperationResult в новую форму с помощью функции mapError
        /// </summary>
        public static OperationResult<TOut> SelectOrElse<TInternal, TOut>(this OperationResult<TInternal> operationResult, [NotNull] Func<TInternal, TOut> map, [NotNull] Func<ActionStatus, IList<OperationError>, TOut> mapError)
        {
            if (operationResult.Success)
                return new OperationResult<TOut>(map(operationResult.Value));
            return new OperationResult<TOut>(mapError(operationResult.ActionStatus, operationResult.Errors));
        }
        
        /// <summary>
        /// Проецирует ошибку из OperationResult в новое значение с помощью функции mapErrorToFallback
        /// </summary>
        public static OperationResult<TInternal> Fallback<TInternal>(this OperationResult<TInternal> operationResult, [NotNull] Func<IList<OperationError>, TInternal> mapErrorToFallback)
        {
            if (!operationResult.Success)
                return new OperationResult<TInternal>(mapErrorToFallback(operationResult.Errors));
            return operationResult;
        }
        
        /// <summary>
        /// Посещает значение OperationResult c помощью inspectAction
        /// </summary>
        public static OperationResult<TInternal> InspectSuccess<TInternal>(this OperationResult<TInternal> operationResult, [NotNull] Action<TInternal> inspectAction)
        {
            if (operationResult.Success)
                inspectAction(operationResult.Value);
            return operationResult;
        }
        
        /// <summary>
        /// Посещает ошибку OperationResult c помощью inspectAction
        /// </summary>
        public static OperationResult<TInternal> InspectError<TInternal>(this OperationResult<TInternal> operationResult, [NotNull] Action<ActionStatus, IList<OperationError>> inspectAction)
        {
            if (!operationResult.Success)
                inspectAction(operationResult.ActionStatus, operationResult.Errors);
            return operationResult;
        }
        
        /// <summary>
        /// Вычисляет является операции успешной и выполняется ли для нее предикат 
        /// </summary>
        public static bool IsSuccessWith<TInternal>(this OperationResult<TInternal> operationResult, [NotNull] Func<TInternal, bool> predicate)
        {
            return operationResult.Select(predicate).Value;
        }

        #region Async
        
        /// <summary>
        /// Преобразует значение OperationResult в асинхронную задачу с помощью функции map.
        /// Перемещает ошибку из исходного OperationResult в OperationResult, получаемый в результате выполнения асинхронной операции
        /// </summary>
        public static Task<OperationResult<TOut>> SelectAsync<TInternal, TOut>(this OperationResult<TInternal> operationResult, [NotNull] Func<TInternal, Task<OperationResult<TOut>>> map)
        {
            if (operationResult.Success)
                return map(operationResult.Value);
            return Task.FromResult(new OperationResult<TOut>(operationResult));
        }
        
        /// <summary>
        /// Проецирует значение OperationResult в асинхронную задачу с помощью функции map.
        /// Заменяет ошибку в исходном OperationResult на defaultValue
        /// </summary>
        public static Task<OperationResult<TOut>> SelectOrAsync<TInternal, TOut>(this OperationResult<TInternal> operationResult, [NotNull] Func<TInternal, Task<OperationResult<TOut>>> map, [NotNull] TOut defaultValue)
        {
            if (operationResult.Success)
                return map(operationResult.Value);
            return Task.FromResult(new OperationResult<TOut>(defaultValue));
        }
        
        /// <summary>
        /// Проецирует значение OperationResult в асинхронную задачу с помощью функции map.
        /// Проецирует ошибку из OperationResult в асинхронную задачу с помощью функции mapError
        /// </summary>
        public static Task<OperationResult<TOut>> SelectOrElseAsync<TInternal, TOut>(this OperationResult<TInternal> operationResult, [NotNull] Func<TInternal, Task<OperationResult<TOut>>> map, [NotNull] Func<ActionStatus, IList<OperationError>, Task<OperationResult<TOut>>> mapError)
        {
            if (operationResult.Success)
                return map(operationResult.Value);
            return mapError(operationResult.ActionStatus, operationResult.Errors);
        }

        /// <summary>
        /// Проецирует ошибку из OperationResult в асинхронную операцию с помощью функции mapErrorToFallback
        /// </summary>
        public static Task<OperationResult<TInternal>> FallbackAsync<TInternal>(this OperationResult<TInternal> operationResult, [NotNull] Func<IList<OperationError>, Task<OperationResult<TInternal>>> mapErrorToFallback)
        {
            if (!operationResult.Success)
                return mapErrorToFallback(operationResult.Errors);
            return Task.FromResult(operationResult);
        }

        #endregion
    }
}