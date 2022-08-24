#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;

namespace ATI.Services.Common.Extensions;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class OperationResultExtensions
{
    public static IOperationExecutor<TResult> Map<TSource, TResult>(this OperationResult<TSource> source, Func<TSource, TResult> map)
    {
        return new OperationResultSelector<TSource, TResult>(source, map);
    }

    public static IOperationExecutor<TResult> MapBi<TFirst, TSecond, TResult>(this OperationResult<TFirst> first, OperationResult<TSecond> second, Func<TFirst, TSecond, TResult> mapBi)
    {
        return first.Map(f => second.Map(s => mapBi(f, s))).UnwrapLazy(second);
    }

    public static IOperationExecutor<TResult> MapBi<TFirst, TSecond, TResult>(this OperationResult<TFirst> first, IOperationExecutor<TSecond> second, Func<TFirst, TSecond, TResult> mapBi)
    {
        return first.Map(f => second.Map(s => mapBi(f, s))).UnwrapLazy(second.GetInitialOperationResult());
    }

    public static IOperationExecutorAsync<TResult> MapAsync<TSource, TResult>(this OperationResult<TSource> source, Func<TSource, Task<TResult>> map)
    {
        return new OperationResultAsyncSelector<TSource, TResult>(source, map);
    }

    public static IOperationExecutorAsync<TResult> MapBiAsync<TFirst, TSecond, TResult>(this OperationResult<TFirst> first, OperationResult<TSecond> second, Func<TFirst, TSecond, Task<TResult>> mapBi)
    {
        return first.MapBi(second, mapBi).AsAsync();
    }

    public static IOperationExecutorAsync<TResult> MapBiAsync<TFirst, TSecond, TResult>(this OperationResult<TFirst> first, IOperationExecutor<TSecond> second, Func<TFirst, TSecond, Task<TResult>> mapBi)
    {
        return first.MapBi(second, mapBi).AsAsync();
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
    /// Выполняет onSuccess для значения внутри OperationResult когда OperationResult.Success is true
    /// </summary>
    public static OperationResult<TSource> InvokeOnSuccess<TSource>(this OperationResult<TSource> source, Action<TSource> onSuccess)
    {
        if (!source.Success)
            return source;

        onSuccess(source.Value);
        return source;
    }

    /// <summary>
    /// Выполняет onError для значения внутри OperationResult когда OperationResult.Success is false
    /// </summary>
    public static OperationResult<TSource> InvokeOnError<TSource>(this OperationResult<TSource> source, Action<ActionStatus, IList<OperationError>> onError)
    {
        if (source.Success)
            return source;

        onError(source.ActionStatus, source.Errors);
        return source;
    }

    /// <summary>
    /// Вычисляет является операции успешной и выполняется ли для нее предикат
    /// </summary>
    public static bool IsSuccessWith<TSource>(this OperationResult<TSource> source, Func<TSource, bool> predicate)
    {
        return source.Success && predicate(source.Value);
    }

    public static OperationResult<TValue> Unwrap<TValue>(this OperationResult<OperationResult<TValue>> source)
    {
        return source.Success ? source.Value : new OperationResult<TValue>(source);
    }
}