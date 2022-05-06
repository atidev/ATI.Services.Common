using System;
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

        public static ILazyEvaluateAsync<TResult> Select2Async<TFirst, TSecond, TResult>(this ILazyEvaluateAsync<TFirst> first,
            ILazyEvaluate<TSecond> second, Func<TFirst, TSecond, Task<TResult>> map2)
        {
            return first.Select(f => second.SelectAsync(s => map2(f, s))).Unwrap(second.GetInitialOperationResult());
        }

        public static ILazyEvaluateAsync<TResult> Select2Async<TFirst, TSecond, TResult>(this ILazyEvaluateAsync<TFirst> first,
            OperationResult<TSecond> second, Func<TFirst, TSecond, Task<TResult>> map2)
        {
            return first.Select(f => second.SelectAsync(s => map2(f, s))).Unwrap(second);
        }

        public static ILazyEvaluateAsync<OperationResult> SelectAsync<TSource>(this ILazyEvaluateAsync<OperationResult<TSource>> source, Func<TSource, Task<OperationResult>> map)
        {
            return new OperationResultAsyncSelector<OperationResult<TSource>, OperationResult>(source, opResult => opResult.SelectAsync(map).AsTaskOr(opResult));
        }

        public static ILazyEvaluateAsync<OperationResult[]> SelectAsync<TSource>(this ILazyEvaluateAsync<OperationResult<TSource>> source, Func<TSource, Task<OperationResult[]>> map)
        {
            return new OperationResultAsyncSelector<OperationResult<TSource>, OperationResult[]>(source, opResult => opResult.SelectAsync(map).AsTaskOr(new OperationResult[]{opResult}));
        }
        
        public static ILazyEvaluateAsync<OperationResult<TResult>> SelectAsync<TSource, TResult>(this ILazyEvaluateAsync<OperationResult<TSource>> source, Func<TSource, Task<OperationResult<TResult>>> map)
        {
            return new OperationResultAsyncSelector<OperationResult<TSource>, OperationResult<TResult>>(source, opResult => opResult.SelectAsync(map).AsTask());
        }

        public static ILazyEvaluateAsync<OperationResult<TResult>[]> SelectAsync<TSource, TResult>(this ILazyEvaluateAsync<OperationResult<TSource>> source, Func<TSource, Task<OperationResult<TResult>[]>> map)
        {
            return new OperationResultAsyncSelector<OperationResult<TSource>, OperationResult<TResult>[]>(source, opResult => opResult.SelectAsync(map).AsTask());
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

        public static Task<TValue> AsTaskOr<TValue>(this ILazyEvaluateAsync<TValue> source, TValue defaultValue)
        {
            return source.CanEvaluated() ? source.EvaluateOrThrowAsync() : Task.FromResult(defaultValue);
        }
        
        public static Task<TValue> AsTaskOr<TValue>(this ILazyEvaluateAsync<TValue> source, Task<TValue> defaultTask)
        {
            return source.CanEvaluated() ? source.EvaluateOrThrowAsync() : defaultTask;
        }
        
        public static Task<TValue> AsTaskOr<TValue>(this ILazyEvaluateAsync<TValue> source, Func<OperationResult, TValue> mapResultFromInitialError)
        {
            if (source.CanEvaluated())
                return source.EvaluateOrThrowAsync();
            
            return Task.FromResult(mapResultFromInitialError(source.GetInitialOperationResult()));
        }
        
        public static Task<OperationResult<TValue>[]> AsTask<TValue>(this ILazyEvaluateAsync<OperationResult<TValue>[]> source)
        {
            return source.AsTaskOr(initialErrOperation => new [] {new OperationResult<TValue>(initialErrOperation)});
        }
        
        public static Task<OperationResult<TValue>> AsTask<TValue>(this ILazyEvaluateAsync<OperationResult<TValue>> source)
        {
            return source.AsTaskOr(initialErrOperation => new OperationResult<TValue>(initialErrOperation));
        }
        
        public static Task<OperationResult> AsTask(this ILazyEvaluateAsync<OperationResult> source)
        {
            return source.AsTaskOr(initialErrOperation => initialErrOperation);
        }

        public static async Task<OperationResult<TValue>> ToOperationResultAsync<TValue>(this ILazyEvaluateAsync<TValue> source)
        {
            return source.CanEvaluated()
                ? new OperationResult<TValue>(await source.EvaluateOrThrowAsync())
                : new OperationResult<TValue>(source.GetInitialOperationResult());
        }
        
        public static async Task<ILazyEvaluateAsync<TValue>> UnwrapAsync<TValue>(this ILazyEvaluateAsync<ILazyEvaluateAsync<TValue>> source)
        { 
            if(await source.IsSuccessWithAsync(i => i.CanEvaluated()))
                return new OperationResultAsyncSelector<ILazyEvaluateAsync<TValue>, TValue>(source, i => i.EvaluateOrThrowAsync());
        
            var errorOp = source.CanEvaluated() ? (await source.EvaluateOrThrowAsync()).GetInitialOperationResult() : source.GetInitialOperationResult();
        
            return new OperationResultAsyncSelector<TValue, TValue>(new OperationResult<TValue>(errorOp), Task.FromResult);
        }

        public static ILazyEvaluateAsync<OperationResult<TValue>> EvaluateInternal<TValue>(this ILazyEvaluateAsync<ILazyEvaluateAsync<TValue>> source)
        {
            return new OperationResultAsyncSelector<ILazyEvaluateAsync<TValue>, OperationResult<TValue>>(source, i => i.ToOperationResultAsync());
        }
        
        private static ILazyEvaluateAsync<TValue> Unwrap<TValue>(this ILazyEvaluateAsync<ILazyEvaluateAsync<TValue>> source, OperationResult initialInternalOperationResult)
        { 
            if(source.CanEvaluated() && initialInternalOperationResult.Success)
                return new OperationResultAsyncSelector<ILazyEvaluateAsync<TValue>, TValue>(source, i => i.EvaluateOrThrowAsync());
        
            var errorOp = source.CanEvaluated() ? initialInternalOperationResult : source.GetInitialOperationResult();
        
            return new OperationResultAsyncSelector<TValue, TValue>(new OperationResult<TValue>(errorOp), Task.FromResult);
        }
        
        private static ILazyEvaluateAsync<TResult> Select<TSource, TResult>(this ILazyEvaluateAsync<TSource> source, Func<TSource, TResult> map)
        {
            return new OperationResultAsyncSelector<TSource, TResult>(source, i => Task.FromResult(map(i)));
        }
        
        // public static ILazyEvaluateAsync<OperationResult> Select2Async<TFirst, TSecond>(this ILazyEvaluateAsync<OperationResult<TFirst>> first, OperationResult<TSecond> second, Func<TFirst, TSecond, Task<OperationResult>> map2)
        // {
        //     return first.SelectAsync(f =>
        //     {
        //         return second.SelectAsync(s => map2(f, s)).AsTaskOr(second);
        //     });
        // }
    }
}