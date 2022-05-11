using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;

namespace ATI.Services.Common.Extensions
{
    public static class OperationResultSelectorExtensions
    {
        #region Select
        public static ILazyEvaluate<TResult> Select<TSource, TResult>(this ILazyEvaluate<TSource> source, Func<TSource, TResult> map)
        {
            return new OperationResultSelector<TSource, TResult>(source, map);
        }
        
        public static ILazyEvaluate<OperationResult<TResult>> Select<TSource, TResult>(this ILazyEvaluate<OperationResult<TSource>> source, Func<TSource, OperationResult<TResult>> map)
        {
            return new OperationResultSelector<OperationResult<TSource>, OperationResult<TResult>>(source, i => i.Select(map).Unwrap());
        }
        
        public static ILazyEvaluate<TResult> Select2<TFirst, TSecond, TResult>(this ILazyEvaluate<TFirst> first, ILazyEvaluate<TSecond> second, Func<TFirst, TSecond, TResult> map2)
        {
            return first.Select(f => second.Select(s => map2(f, s))).Unwrap(second.GetInitialOperationResult());
        }        
        
        public static ILazyEvaluate<TResult> Select2<TFirst, TSecond, TResult>(this ILazyEvaluate<TFirst> first, OperationResult<TSecond> second, Func<TFirst, TSecond, TResult> map2)
        {
            return first.Select(f => second.Select(s => map2(f, s))).Unwrap(second);
        }

        public static ILazyEvaluate<TResult> Select2<TFirst, TSecond, TResult>(this ILazyEvaluate<OperationResult<TFirst>> first, OperationResult<TSecond> second, Func<TFirst, TSecond, TResult> map2)
        {
            return first.Select(fOp => fOp.Select2(second, map2)).Unwrap(second);
        }
        #endregion

        #region SelectAsync
        public static ILazyEvaluateAsync<TResult> SelectAsync<TSource, TResult>(this ILazyEvaluate<TSource> source, Func<TSource, Task<TResult>> map)
        {
            return new OperationResultAsyncSelector<TSource, TResult>(source, map);
        }
        
        public static ILazyEvaluateAsync<TResult> Select2Async<TFirst, TSecond, TResult>(this ILazyEvaluate<TFirst> first, ILazyEvaluate<TSecond> second, Func<TFirst, TSecond, Task<TResult>> map2)
        {
            return first.Select2(second, map2).AsAsync();
        }
        
        public static ILazyEvaluateAsync<TResult> Select2Async<TFirst, TSecond, TResult>(this ILazyEvaluate<TFirst> first, OperationResult<TSecond> second, Func<TFirst, TSecond, Task<TResult>> map2)
        {
            return first.Select2(second, map2).AsAsync();
        }
        
        public static ILazyEvaluateAsync<OperationResult<TResult>> Select2Async<TFirst, TSecond, TResult>(this ILazyEvaluate<OperationResult<TFirst>> first, OperationResult<TSecond> second, Func<TFirst, TSecond, Task<OperationResult<TResult>>> map2)
        {
            return first.Select2(second, map2).AsAsync();
        }
        
        public static ILazyEvaluateAsync<TValue> AsAsync<TValue>(this ILazyEvaluate<Task<TValue>> source)
        {
            return source.SelectAsync(i => i);
        }
        #endregion

        #region Evaluate
        /// <summary>
        /// Вычисляет является операции успешной и выполняется ли для нее предикат 
        /// </summary>
        public static bool IsSuccessWith<TValue>(this ILazyEvaluate<TValue> source, [NotNull] Func<TValue, bool> predicate)
        {
            return source.CanEvaluated() && predicate(source.EvaluateOrThrow());
        }

        public static OperationResult<TValue> ToOperationResult<TValue>(this ILazyEvaluate<TValue> source)
        {
            return source.CanEvaluated() ? new OperationResult<TValue>(source.EvaluateOrThrow()) : new OperationResult<TValue>(source.GetInitialOperationResult());
        }
        
        public static OperationResult<TValue> Unwrap<TValue>(this ILazyEvaluate<OperationResult<TValue>> source)
        {
            return source.CanEvaluated() ? source.EvaluateOrThrow() : new OperationResult<TValue>(source.GetInitialOperationResult());
        }

        public static TValue UnwrapOr<TValue>(this ILazyEvaluate<TValue> source, TValue defaultValue)
        {
            return source.CanEvaluated() ? source.EvaluateOrThrow() : defaultValue;
        }
        
        internal static ILazyEvaluate<TValue> Unwrap<TValue>(this ILazyEvaluate<ILazyEvaluate<TValue>> source, OperationResult internalInitial)
        {
            if (source.CanEvaluated() && internalInitial.Success)
                return new OperationResultSelector<ILazyEvaluate<TValue>, TValue>(source, internalLazy => internalLazy.EvaluateOrThrow());

            var errorOp = source.CanEvaluated() ? internalInitial : source.GetInitialOperationResult();
            return new OperationResultSelector<TValue, TValue>(new OperationResult<TValue>(errorOp), i => i);
        }

        #endregion
    }
}