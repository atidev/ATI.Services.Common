using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;

namespace ATI.Services.Common.Extensions
{
    public static class OperationResultSelectorExtensions
    {
        public static ILazyEvaluate<TResult> Select<TSource, TResult>(this ILazyEvaluate<TSource> source, Func<TSource, TResult> map)
        {
            return new OperationResultSelector<TSource, TResult>(source, map);
        }
        
        /// <summary>
        /// Посещает значение OperationResult c помощью inspectAction
        /// </summary>
        public static ILazyEvaluate<TSource> InspectSuccess<TSource>(this ILazyEvaluate<TSource> source, [NotNull] Action<TSource> inspectAction)
        {
            return new OperationResultSelector<TSource, TSource>(source, i =>
            {
                inspectAction(i);
                return i;
            });
        }
        
        /// <summary>
        /// Посещает значение OperationResult c помощью inspectAction
        /// </summary>
        public static ILazyEvaluate<TSource> InspectError<TSource>(this ILazyEvaluate<TSource> source, [NotNull] Action<ActionStatus, IList<OperationError>> inspectAction)
        {
            if (source.CanEvaluated()) return source;
            
            var initial = source.GetInitialOperationResult();
            inspectAction(initial.ActionStatus, initial.Errors);
            return source;
        }
        
        public static ILazyEvaluateAsync<TResult> SelectAsync<TSource, TResult>(this ILazyEvaluate<TSource> source, Func<TSource, Task<TResult>> map)
        {
            return new OperationResultAsyncSelector<TSource, TResult>(source, map);
        }

        /// <summary>
        /// Вычисляет является операции успешной и выполняется ли для нее предикат 
        /// </summary>
        public static bool IsSuccessWith<TOut>(this ILazyEvaluate<TOut> source, [NotNull] Func<TOut, bool> predicate)
        {
            return source.CanEvaluated() && predicate(source.EvaluateOrThrow());
        }

        public static OperationResult<TOut> ToOperationResult<TOut>(this ILazyEvaluate<TOut> source)
        {
            return source.CanEvaluated() ? new OperationResult<TOut>(source.EvaluateOrThrow()) : new OperationResult<TOut>(source.GetInitialOperationResult());
        }
        
        public static OperationResult<TValue> Unwrap<TValue>(this ILazyEvaluate<OperationResult<TValue>> source)
        {
            return source.CanEvaluated() ? source.EvaluateOrThrow() : new OperationResult<TValue>(source.GetInitialOperationResult());
        }

        public static TOut UnwrapOr<TOut>(this ILazyEvaluate<TOut> source, TOut defaultValue)
        {
            return source.CanEvaluated() ? source.EvaluateOrThrow() : defaultValue;
        }
    }
}