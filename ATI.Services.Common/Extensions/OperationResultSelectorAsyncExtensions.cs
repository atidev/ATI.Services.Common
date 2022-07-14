using System;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;

namespace ATI.Services.Common.Extensions
{
    public static class OperationResultSelectorAsyncExtensions
    {
        public static ILazyEvaluateAsync<TResult> MapAsync<TSource, TResult>(this ILazyEvaluateAsync<TSource> source, Func<TSource, Task<TResult>> map)
        {
            return new OperationResultAsyncSelector<TSource, TResult>(source, map);
        }

        public static ILazyEvaluateAsync<TResult> Map2Async<TFirst, TSecond, TResult>(this ILazyEvaluateAsync<TFirst> first,
            ILazyEvaluate<TSecond> second, Func<TFirst, TSecond, Task<TResult>> map2)
        {
            return first.Map(f => second.MapAsync(s => map2(f, s))).UnwrapLazy(second.GetInitialOperationResult());
        }

        public static ILazyEvaluateAsync<TResult> Map2Async<TFirst, TSecond, TResult>(this ILazyEvaluateAsync<TFirst> first,
            OperationResult<TSecond> second, Func<TFirst, TSecond, Task<TResult>> map2)
        {
            return first.Map(f => second.MapAsync(s => map2(f, s))).UnwrapLazy(second);
        }

        public static ILazyEvaluateAsync<OperationResult> MapAsync<TSource>(this ILazyEvaluateAsync<OperationResult<TSource>> source, Func<TSource, Task<OperationResult>> map)
        {
            return new OperationResultAsyncSelector<OperationResult<TSource>, OperationResult>(source, opResult => opResult.MapAsync(map).AsTaskOrDefault(opResult));
        }

        public static ILazyEvaluateAsync<OperationResult[]> MapAsync<TSource>(this ILazyEvaluateAsync<OperationResult<TSource>> source, Func<TSource, Task<OperationResult[]>> map)
        {
            return new OperationResultAsyncSelector<OperationResult<TSource>, OperationResult[]>(source, opResult => opResult.MapAsync(map).AsTaskOrDefault(new OperationResult[]{opResult}));
        }
        
        public static ILazyEvaluateAsync<OperationResult<TResult>> MapAsync<TSource, TResult>(this ILazyEvaluateAsync<OperationResult<TSource>> source, Func<TSource, Task<OperationResult<TResult>>> map)
        {
            return new OperationResultAsyncSelector<OperationResult<TSource>, OperationResult<TResult>>(source, opResult => opResult.MapAsync(map).AsTask());
        }

        public static ILazyEvaluateAsync<OperationResult<TResult>[]> MapAsync<TSource, TResult>(this ILazyEvaluateAsync<OperationResult<TSource>> source, Func<TSource, Task<OperationResult<TResult>[]>> map)
        {
            return new OperationResultAsyncSelector<OperationResult<TSource>, OperationResult<TResult>[]>(source, opResult => opResult.MapAsync(map).AsTask());
        }
        
        /// <summary>
        /// Вычисляет является операции успешной и выполняется ли для нее предикат 
        /// </summary>
        public static async Task<bool> IsSuccessWithAsync<TOut>(this ILazyEvaluateAsync<TOut> source, [NotNull] Func<TOut, bool> predicate)
        {
            if (!source.CanEvaluated()) return false;
            var evaluated = await source.EvaluateOrThrowAsync();
            return predicate(evaluated); 
        }

        public static Task<TValue> AsTaskOrDefault<TValue>(this ILazyEvaluateAsync<TValue> source, TValue defaultValue)
        {
            return source.CanEvaluated() ? source.EvaluateOrThrowAsync() : Task.FromResult(defaultValue);
        }
        
        public static Task<TValue> AsTaskOrDefault<TValue>(this ILazyEvaluateAsync<TValue> source, Task<TValue> defaultTask)
        {
            return source.CanEvaluated() ? source.EvaluateOrThrowAsync() : defaultTask;
        }
        
        public static Task<TValue> AsTaskOrDefault<TValue>(this ILazyEvaluateAsync<TValue> source, Func<OperationResult, TValue> mapResultFromInitialError)
        {
            if (source.CanEvaluated())
                return source.EvaluateOrThrowAsync();
            
            return Task.FromResult(mapResultFromInitialError(source.GetInitialOperationResult()));
        }
        
        public static Task<OperationResult<TValue>[]> AsTask<TValue>(this ILazyEvaluateAsync<OperationResult<TValue>[]> source)
        {
            return source.AsTaskOrDefault(initialErrOperation => new [] {new OperationResult<TValue>(initialErrOperation)});
        }
        
        public static Task<OperationResult<TValue>> AsTask<TValue>(this ILazyEvaluateAsync<OperationResult<TValue>> source)
        {
            return source.AsTaskOrDefault(initialErrOperation => new OperationResult<TValue>(initialErrOperation));
        }
        
        public static Task<OperationResult> AsTask(this ILazyEvaluateAsync<OperationResult> source)
        {
            return source.AsTaskOrDefault(initialErrOperation => initialErrOperation);
        }

        public static async Task<OperationResult<TValue>> ToOperationResultAsync<TValue>(this ILazyEvaluateAsync<TValue> source)
        {
            return source.CanEvaluated()
                ? new OperationResult<TValue>(await source.EvaluateOrThrowAsync())
                : new OperationResult<TValue>(source.GetInitialOperationResult());
        }

        private static ILazyEvaluateAsync<TValue> UnwrapLazy<TValue>(this ILazyEvaluateAsync<ILazyEvaluateAsync<TValue>> source, OperationResult initialInternalOperationResult)
        { 
            if(source.CanEvaluated() && initialInternalOperationResult.Success)
                return new OperationResultAsyncSelector<ILazyEvaluateAsync<TValue>, TValue>(source, i => i.EvaluateOrThrowAsync());
        
            var errorOp = source.CanEvaluated() ? initialInternalOperationResult : source.GetInitialOperationResult();
        
            return new OperationResultAsyncSelector<TValue, TValue>(new OperationResult<TValue>(errorOp), Task.FromResult);
        }
        
        private static ILazyEvaluateAsync<TResult> Map<TSource, TResult>(this ILazyEvaluateAsync<TSource> source, Func<TSource, TResult> map)
        {
            return new OperationResultAsyncSelector<TSource, TResult>(source, i => Task.FromResult(map(i)));
        }
    }
}