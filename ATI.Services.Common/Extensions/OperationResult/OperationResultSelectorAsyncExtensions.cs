using System;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;

namespace ATI.Services.Common.Extensions.OperationResult
{
    public static class OperationResultSelectorAsyncExtensions
    {
        public static ILazyEvaluateAsync<TResult> SelectAsync<TSource, TResult>(this ILazyEvaluateAsync<TSource> source, Func<TSource, Task<TResult>> map)
        {
            return new OperationResultAsyncSelector<TSource, TResult>(source, map);
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

        public static async Task<TOut> AsTaskOr<TOut>(this ILazyEvaluateAsync<TOut> source, TOut defaultValue)
        {
            if (!source.CanEvaluated())
                return defaultValue;
            try
            {
                return await source.EvaluateOrThrowAsync();
            }
            catch
            {
                return defaultValue;
            }
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