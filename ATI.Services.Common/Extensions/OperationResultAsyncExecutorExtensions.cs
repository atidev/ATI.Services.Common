#nullable enable
using System;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;

namespace ATI.Services.Common.Extensions;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class OperationResultAsyncExecutorExtensions
{
    #region MapAsync

    public static IOperationExecutorAsync<TResult> MapAsync<TSource, TResult>(this IOperationExecutorAsync<TSource> source, Func<TSource, Task<TResult>> map)
    {
        return new OperationResultAsyncExecutor<TSource, TResult>(source, map);
    }

    public static IOperationExecutorAsync<OperationResult> MapAsync<TSource>(this IOperationExecutorAsync<OperationResult<TSource>> source, Func<TSource, Task<OperationResult>> map)
    {
        return new OperationResultAsyncExecutor<OperationResult<TSource>, OperationResult>(source, opResult => opResult.MapAsync(map).AsTaskOrDefault(opResult));
    }

    public static IOperationExecutorAsync<OperationResult[]> MapAsync<TSource>(this IOperationExecutorAsync<OperationResult<TSource>> source, Func<TSource, Task<OperationResult[]>> map)
    {
        return new OperationResultAsyncExecutor<OperationResult<TSource>, OperationResult[]>(source, opResult => opResult.MapAsync(map).AsTaskOrDefault(new OperationResult[]{opResult}));
    }
        
    public static IOperationExecutorAsync<OperationResult<TResult>> MapAsync<TSource, TResult>(this IOperationExecutorAsync<OperationResult<TSource>> source, Func<TSource, Task<OperationResult<TResult>>> map)
    {
        return new OperationResultAsyncExecutor<OperationResult<TSource>, OperationResult<TResult>>(source, opResult => opResult.MapAsync(map).AsTask());
    }

    public static IOperationExecutorAsync<OperationResult<TResult>[]> MapAsync<TSource, TResult>(this IOperationExecutorAsync<OperationResult<TSource>> source, Func<TSource, Task<OperationResult<TResult>[]>> map)
    {
        return new OperationResultAsyncExecutor<OperationResult<TSource>, OperationResult<TResult>[]>(source, opResult => opResult.MapAsync(map).AsTask());
    }
        
    public static IOperationExecutorAsync<TResult> MapBiAsync<TFirst, TSecond, TResult>(this IOperationExecutorAsync<TFirst> first, IOperationExecutor<TSecond> second, Func<TFirst, TSecond, Task<TResult>> mapBi)
    {
        return first.Map(f => second.MapAsync(s => mapBi(f, s))).UnwrapLazy(second.GetInitialOperationResult());
    }

    public static IOperationExecutorAsync<TResult> MapBiAsync<TFirst, TSecond, TResult>(this IOperationExecutorAsync<TFirst> first, OperationResult<TSecond> second, Func<TFirst, TSecond, Task<TResult>> mapBi)
    {
        return first.Map(f => second.MapAsync(s => mapBi(f, s))).UnwrapLazy(second);
    }

    #endregion
        
    #region Evaluate

    public static Task<TValue> AsTaskOrDefault<TValue>(this IOperationExecutorAsync<TValue> source, TValue defaultValue)
    {
        return source.CanExecuted() ? source.ExecuteAsync() : Task.FromResult(defaultValue);
    }
        
    public static Task<TValue> AsTaskOrDefault<TValue>(this IOperationExecutorAsync<TValue> source, Task<TValue> defaultTask)
    {
        return source.CanExecuted() ? source.ExecuteAsync() : defaultTask;
    }

    public static Task<TValue> AsTaskOrDefault<TValue>(this IOperationExecutorAsync<TValue> source, Func<OperationResult, TValue> mapResultFromInitialError)
    {
        if (source.CanExecuted())
            return source.ExecuteAsync();
            
        return Task.FromResult(mapResultFromInitialError(source.GetInitialOperationResult()));
    }
        
    public static Task<OperationResult<TValue>[]> AsTask<TValue>(this IOperationExecutorAsync<OperationResult<TValue>[]> source)
    {
        return source.AsTaskOrDefault(initialErrOperation => new [] {new OperationResult<TValue>(initialErrOperation)});
    }
        
    public static Task<OperationResult<TValue>> AsTask<TValue>(this IOperationExecutorAsync<OperationResult<TValue>> source)
    {
        return source.AsTaskOrDefault(initialErrOperation => new OperationResult<TValue>(initialErrOperation));
    }
        
    public static Task<OperationResult> AsTask(this IOperationExecutorAsync<OperationResult> source)
    {
        return source.AsTaskOrDefault(initialErrOperation => initialErrOperation);
    }

    public static async Task<OperationResult<TValue>> ToOperationResultAsync<TValue>(this IOperationExecutorAsync<TValue> source)
    {
        return source.CanExecuted()
            ? new OperationResult<TValue>(await source.ExecuteAsync())
            : new OperationResult<TValue>(source.GetInitialOperationResult());
    }

    #endregion

    private static IOperationExecutorAsync<TValue> UnwrapLazy<TValue>(this IOperationExecutorAsync<IOperationExecutorAsync<TValue>> source, OperationResult initialInternalOperationResult)
    { 
        if(source.CanExecuted() && initialInternalOperationResult.Success)
            return new OperationResultAsyncExecutor<IOperationExecutorAsync<TValue>, TValue>(source, i => i.ExecuteAsync());
        
        var errorOp = source.CanExecuted() ? initialInternalOperationResult : source.GetInitialOperationResult();
        
        return new OperationResultAsyncExecutor<TValue, TValue>(new OperationResult<TValue>(errorOp), Task.FromResult);
    }
        
    private static IOperationExecutorAsync<TResult> Map<TSource, TResult>(this IOperationExecutorAsync<TSource> source, Func<TSource, TResult> map)
    {
        return new OperationResultAsyncExecutor<TSource, TResult>(source, i => Task.FromResult(map(i)));
    }
}