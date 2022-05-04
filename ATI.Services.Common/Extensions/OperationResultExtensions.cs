using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;

namespace ATI.Services.Common.Extensions
{
    public static class OperationResultExtensions 
    {
        public static ILazyEvaluate<TOut> Select<TInternal, TOut>(this OperationResult<TInternal> source, Func<TInternal, TOut> map)
        {
            return new OperationResultSelector<TInternal, TOut>(source, map);
        }
        
        public static ILazyEvaluateAsync<TOut> SelectAsync<TInternal, TOut>(this OperationResult<TInternal> source, Func<TInternal, Task<TOut>> map)
        {
            return new OperationResultAsyncSelector<TInternal, TOut>(source, map);
        }

        public static Task<OperationResult<TSource>> FallbackAsync<TSource>(this OperationResult<TSource> source, Func<ActionStatus, IList<OperationError>, Task<OperationResult<TSource>>> map)
        {
            return !source.Success ? map(source.ActionStatus, source.Errors) : Task.FromResult(source);
        }

        /// <summary>
        /// Посещает значение OperationResult c помощью inspectAction
        /// </summary>
        public static OperationResult<TInternal> InspectSuccess<TInternal>(this OperationResult<TInternal> source, [NotNull] Action<TInternal> inspectAction)
        {
            if (!source.Success)
                return source;

            inspectAction(source.Value);
            return source;
        }
        
        /// <summary>
        /// Посещает значение OperationResult c помощью inspectAction
        /// </summary>
        public static OperationResult<TInternal> InspectError<TInternal>(this OperationResult<TInternal> source, [NotNull] Action<ActionStatus, IList<OperationError>> inspectAction)
        {
            if (source.Success)
                return source;

            inspectAction(source.ActionStatus, source.Errors);
            return source;
        }
        
        /// <summary>
        /// Вычисляет является операции успешной и выполняется ли для нее предикат 
        /// </summary>
        public static bool IsSuccessWith<TInternal>(this OperationResult<TInternal> source, [NotNull] Func<TInternal, bool> predicate)
        {
            if (source.Success)
                return predicate(source.Value);
            return false;
        }
    }
}