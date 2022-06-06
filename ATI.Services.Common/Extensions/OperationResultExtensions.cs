using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;

namespace ATI.Services.Common.Extensions
{
    public static class OperationResultExtensions 
    {
        public static ILazyEvaluate<TResult> Map<TSource, TResult>(this OperationResult<TSource> source, Func<TSource, TResult> map)
        {
            return new OperationResultSelector<TSource, TResult>(source, map);
        }
        
        public static ILazyEvaluate<TResult> Map2<TFirst, TSecond, TResult>(this OperationResult<TFirst> first, OperationResult<TSecond> second, Func<TFirst, TSecond, TResult> map2)
        {
            return first.Map(f => second.Map(s => map2(f, s))).UnwrapLazy(second);
        }
        
        public static ILazyEvaluate<TResult> Map2<TFirst, TSecond, TResult>(this OperationResult<TFirst> first, ILazyEvaluate<TSecond> second, Func<TFirst, TSecond, TResult> map2)
        {
            return first.Map(f => second.Map(s => map2(f, s))).UnwrapLazy(second.GetInitialOperationResult());
        }
        
        public static ILazyEvaluateAsync<TResult> MapAsync<TSource, TResult>(this OperationResult<TSource> source, Func<TSource, Task<TResult>> map)
        {
            return new OperationResultAsyncSelector<TSource, TResult>(source, map);
        }
        
        public static ILazyEvaluateAsync<TResult> Map2Async<TFirst, TSecond, TResult>(this OperationResult<TFirst> first, OperationResult<TSecond> second, Func<TFirst, TSecond, Task<TResult>> map2)
        {
            return first.Map2(second, map2).AsAsync();
        }
        
        public static ILazyEvaluateAsync<TResult> Map2Async<TFirst, TSecond, TResult>(this OperationResult<TFirst> first, ILazyEvaluate<TSecond> second, Func<TFirst, TSecond, Task<TResult>> map2)
        {
            return first.Map2(second, map2).AsAsync();
        }
        
        public static OperationResult<TSource> Fallback<TSource>(this OperationResult<TSource> source, Func<ActionStatus, IList<OperationError>, OperationResult<TSource>> fallback)
        {
            return !source.Success ? fallback(source.ActionStatus, source.Errors) : source;
        }

        public static Task<OperationResult<TSource>> FallbackAsync<TSource>(this OperationResult<TSource> source, Func<ActionStatus, IList<OperationError>, Task<OperationResult<TSource>>> fallback)
        {
            return !source.Success ? fallback(source.ActionStatus, source.Errors) : Task.FromResult(source);
        }

        /// <summary>
        /// Посещает значение OperationResult c помощью inspectAction
        /// </summary>
        public static OperationResult<TSource> InspectSuccess<TSource>(this OperationResult<TSource> source, [NotNull] Action<TSource> inspectAction)
        {
            if (!source.Success)
                return source;

            inspectAction(source.Value);
            return source;
        }
        
        /// <summary>
        /// Посещает значение OperationResult c помощью inspectAction
        /// </summary>
        public static OperationResult<TSource> InspectError<TSource>(this OperationResult<TSource> source, [NotNull] Action<ActionStatus, IList<OperationError>> inspectAction)
        {
            if (source.Success)
                return source;

            inspectAction(source.ActionStatus, source.Errors);
            return source;
        }

        /// <summary>
        /// Вычисляет является операции успешной и выполняется ли для нее предикат 
        /// </summary>
        public static bool IsSuccessWith<TSource>(this OperationResult<TSource> source, [NotNull] Func<TSource, bool> predicate)
        {
            if (source.Success)
                return predicate(source.Value);
            return false;
        }

        public static OperationResult<TValue> Unwrap<TValue>(this OperationResult<OperationResult<TValue>> source)
        {
            return source.Success ? source.Value : new OperationResult<TValue>(source);
        }
    }
}