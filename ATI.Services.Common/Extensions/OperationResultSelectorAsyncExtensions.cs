using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;

namespace ATI.Services.Common.Extensions
{
    public static class OperationResultSelectorAsyncExtensions
    {
        public static ILazyEvaluateAsync<TResult> SelectAsync<TSource, TResult>(this ILazyEvaluateAsync<TSource> source, Func<TSource, Task<TResult>> map)
        {
            return new OperationResultAsyncSelector<TSource, TResult>(source, map);
        }

        public static ILazyEvaluateAsync<OperationResult> SelectAsync<TSource>(this ILazyEvaluateAsync<OperationResult<TSource>> source, Func<TSource, Task<OperationResult>> map)
        {
            return new OperationResultAsyncSelector<OperationResult<TSource>, OperationResult>(source, opResult => opResult.SelectAsync(map).AsTaskOr(opResult));
        }
        
        public static ILazyEvaluateAsync<IEnumerable<OperationResult>> SelectAsync<TSource>(this ILazyEvaluateAsync<OperationResult<IEnumerable<TSource>>> source, Func<IEnumerable<TSource>, Task<IEnumerable<OperationResult>>> map)
        {
            return new OperationResultAsyncSelector<OperationResult<IEnumerable<TSource>>, IEnumerable<OperationResult>>(source, opResult => opResult.SelectAsync(map).AsTaskOr(new []{opResult}));
        }
        
        public static ILazyEvaluateAsync<OperationResult<TResult>> SelectAsync<TSource, TResult>(this ILazyEvaluateAsync<OperationResult<TSource>> source, Func<TSource, Task<OperationResult<TResult>>> map)
        {
            return new OperationResultAsyncSelector<OperationResult<TSource>, OperationResult<TResult>>(source, opResult => opResult.SelectAsync(map).AsTaskOr(new OperationResult<TResult>(opResult)));
        }
        
        public static ILazyEvaluateAsync<IEnumerable<OperationResult<TResult>>> SelectAsync<TSource, TResult>(this ILazyEvaluateAsync<OperationResult<TSource>> source, Func<TSource, Task<IEnumerable<OperationResult<TResult>>>> map)
        {
            return new OperationResultAsyncSelector<OperationResult<TSource>, IEnumerable<OperationResult<TResult>>>(source, opResult => opResult.SelectAsync(map).AsTaskOr(new []{new OperationResult<TResult>(opResult)}));
        }
        
        /// <summary>
        /// Вычисляет является операции успешной и выполняется ли для нее предикат 
        /// </summary>
        public static async Task<bool> IsSuccessWithAsync<TOut>(this ILazyEvaluateAsync<TOut> source, [NotNull] Func<TOut, bool> predicate)
        {
            if (!source.CanEvaluated()) return false;
            try
            {
                var evaluated = await source.EvaluateOrThrowAsync();
                return predicate(evaluated);
            }
            catch
            {
                return false;
            }
        }
        
        public static async Task<TOut> EvaluateOrAsync<TOut>(this ILazyEvaluateAsync<TOut> source, TOut defaultValue)
        {
            if (!source.CanEvaluated()) return defaultValue;
            try
            {
                return await source.EvaluateOrThrowAsync();
            }
            catch
            {
                return defaultValue;
            }
        }

        public static Task<OperationResult<TValue>> AsTask<TValue>(this ILazyEvaluateAsync<OperationResult<TValue>> source)
        {
            return source.AsTaskOr(initialErrOperation => new OperationResult<TValue>(initialErrOperation));
        }
        
        public static Task<OperationResult> AsTask(this ILazyEvaluateAsync<OperationResult> source)
        {
            return source.AsTaskOr(initialErrOperation => initialErrOperation);
        }
        
        public static Task<TValue> AsTaskOr<TValue>(this ILazyEvaluateAsync<TValue> source, TValue defaultValue)
        {
            return source.CanEvaluated() ? source.EvaluateOrThrowAsync() : Task.FromResult(defaultValue);
        }
        
        public static Task<TValue> AsTaskOr<TValue>(this ILazyEvaluateAsync<TValue> source, Func<OperationResult, TValue> mapResultFromInitialError)
        {
            if (source.CanEvaluated())
                return source.EvaluateOrThrowAsync();
            
            return Task.FromResult(mapResultFromInitialError(source.GetInitialOperationResult()));
        }

        public static async Task<OperationResult<TOut>> ToOperationResultAsync<TOut>(this ILazyEvaluateAsync<TOut> source)
        {
            if (!source.CanEvaluated()) 
                return new OperationResult<TOut>(source.GetInitialOperationResult());
            try
            {
                return new OperationResult<TOut>(await source.EvaluateOrThrowAsync());
            }
            catch (Exception ex)
            {
                return new OperationResult<TOut>(ActionStatus.InternalServerError, ex.Message);
            }
        }
    }
}