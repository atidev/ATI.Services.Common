using System;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;

namespace ATI.Services.Common.Extensions;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public static class OperationResultSelectorExtensions
{
    #region Map
    public static IOperationExecutor<TResult> Map<TSource, TResult>(this IOperationExecutor<TSource> source, Func<TSource, TResult> map)
    {
        return new OperationResultSelector<TSource, TResult>(source, map);
    }
        
    public static IOperationExecutor<OperationResult<TResult>> Map<TSource, TResult>(this IOperationExecutor<OperationResult<TSource>> source, Func<TSource, OperationResult<TResult>> map)
    {
        return new OperationResultSelector<OperationResult<TSource>, OperationResult<TResult>>(source, i => i.Map(map).Unwrap());
    }
        
    public static IOperationExecutor<OperationResult<TResult>> Map<TSource, TResult>(this IOperationExecutor<OperationResult<TSource>> source, Func<TSource, TResult> map)
    {
        return new OperationResultSelector<OperationResult<TSource>, OperationResult<TResult>>(source, i => i.Map(map).ToOperationResult());
    }

    public static IOperationExecutor<TResult> MapBi<TFirst, TSecond, TResult>(this IOperationExecutor<TFirst> first, IOperationExecutor<TSecond> second, Func<TFirst, TSecond, TResult> mapBi)
    {
        return first.Map(f => second.Map(s => mapBi(f, s))).UnwrapLazy(second.GetInitialOperationResult());
    }        
        
    public static IOperationExecutor<TResult> MapBi<TFirst, TSecond, TResult>(this IOperationExecutor<TFirst> first, OperationResult<TSecond> second, Func<TFirst, TSecond, TResult> mapBi)
    {
        return first.Map(f => second.Map(s => mapBi(f, s))).UnwrapLazy(second);
    }

    public static IOperationExecutor<TResult> MapBi<TFirst, TSecond, TResult>(this IOperationExecutor<OperationResult<TFirst>> first, OperationResult<TSecond> second, Func<TFirst, TSecond, TResult> mapBi)
    {
        return first.Map(fOp => fOp.MapBi(second, mapBi)).UnwrapLazy(second);
    }
    #endregion

    #region MapAsync
    public static IOperationExecutorAsync<TResult> MapAsync<TSource, TResult>(this IOperationExecutor<TSource> source, Func<TSource, Task<TResult>> map)
    {
        return new OperationResultAsyncSelector<TSource, TResult>(source, map);
    }
        
    public static IOperationExecutorAsync<OperationResult<TResult>> MapAsync<TSource, TResult>(this IOperationExecutor<OperationResult<TSource>> source, Func<TSource, Task<OperationResult<TResult>>> map)
    {
        return new OperationResultAsyncSelector<OperationResult<TSource>, OperationResult<TResult>>(source, op => op.MapAsync(map).AsTask());
    }
        
    public static IOperationExecutorAsync<OperationResult> MapAsync<TSource, TResult>(this IOperationExecutor<OperationResult<TSource>> source, Func<TSource, Task<OperationResult>> map)
    {
        return new OperationResultAsyncSelector<OperationResult<TSource>, OperationResult>(source, op => op.MapAsync(map).AsTask());
    }

    public static IOperationExecutorAsync<TResult> MapBiAsync<TFirst, TSecond, TResult>(this IOperationExecutor<TFirst> first, IOperationExecutor<TSecond> second, Func<TFirst, TSecond, Task<TResult>> mapBi)
    {
        return first.MapBi(second, mapBi).AsAsync();
    }
        
    public static IOperationExecutorAsync<TResult> MapBiAsync<TFirst, TSecond, TResult>(this IOperationExecutor<TFirst> first, OperationResult<TSecond> second, Func<TFirst, TSecond, Task<TResult>> mapBi)
    {
        return first.MapBi(second, mapBi).AsAsync();
    }
        
    public static IOperationExecutorAsync<OperationResult<TResult>> MapBiAsync<TFirst, TSecond, TResult>(this IOperationExecutor<OperationResult<TFirst>> first, OperationResult<TSecond> second, Func<TFirst, TSecond, Task<OperationResult<TResult>>> mapBi)
    {
        return first.MapBi(second, mapBi).AsAsync();
    }
        
    public static IOperationExecutorAsync<TValue> AsAsync<TValue>(this IOperationExecutor<Task<TValue>> source)
    {
        return source.MapAsync(i => i);
    }
    #endregion

    #region Evaluate
    /// <summary>
    /// Вычисляет является операции успешной и выполняется ли для нее предикат 
    /// </summary>
    public static bool IsSuccessWith<TValue>(this IOperationExecutor<TValue> source, [NotNull] Func<TValue, bool> predicate)
    {
        return source.CanEvaluated() && predicate(source.Evaluate());
    }

    public static OperationResult<TValue> ToOperationResult<TValue>(this IOperationExecutor<TValue> source)
    {
        return source.CanEvaluated() ? new OperationResult<TValue>(source.Evaluate()) : new OperationResult<TValue>(source.GetInitialOperationResult());
    }
        
    public static OperationResult<TValue> Unwrap<TValue>(this IOperationExecutor<OperationResult<TValue>> source)
    {
        return source.CanEvaluated() ? source.Evaluate() : new OperationResult<TValue>(source.GetInitialOperationResult());
    }

    public static TValue UnwrapOrDefault<TValue>(this IOperationExecutor<TValue> source, TValue defaultValue)
    {
        return source.CanEvaluated() ? source.Evaluate() : defaultValue;
    }
        
    internal static IOperationExecutor<TValue> UnwrapLazy<TValue>(this IOperationExecutor<IOperationExecutor<TValue>> source, OperationResult internalInitial)
    {
        if (source.CanEvaluated() && internalInitial.Success)
            return new OperationResultSelector<IOperationExecutor<TValue>, TValue>(source, internalLazy => internalLazy.Evaluate());

        var errorOp = source.CanEvaluated() ? internalInitial : source.GetInitialOperationResult();
        return new OperationResultSelector<TValue, TValue>(new OperationResult<TValue>(errorOp), i => i);
    }

    #endregion
}