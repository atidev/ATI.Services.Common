#nullable enable
using System;
using System.Threading.Tasks;

namespace ATI.Services.Common.Behaviors;

public sealed class OperationResultAsyncExecutor<TSource, TOut> : IOperationExecutorAsync<TOut>
{
    private readonly IOperationExecutor<TSource>? _previousSync;
    private readonly IOperationExecutorAsync<TSource>? _previous;
    private readonly OperationResult<TSource>? _operationResult;
    private readonly Func<TSource, Task<TOut>> _select;
    private bool IsFirst => _operationResult is not null;
    private bool IsAfterSync => _previousSync is not null;

    public OperationResultAsyncExecutor(IOperationExecutor<TSource> previousSync, Func<TSource, Task<TOut>> select)
    {
        _previousSync = previousSync;
        _select = select;
        _operationResult = null;
        _previous = null;
    }
        
    public OperationResultAsyncExecutor(OperationResult<TSource> operationResult, Func<TSource, Task<TOut>> select)
    {
        _operationResult = operationResult;
        _select = select;
        _previous = null;
        _previousSync = null;
    }

    public OperationResultAsyncExecutor(IOperationExecutorAsync<TSource> previous, Func<TSource, Task<TOut>> select)
    {
        _select = select;
        _previous = previous;
        _operationResult = null;
        _previousSync = null;
    }

    async Task<TOut> IOperationExecutorAsync<TOut>.ExecuteAsync()
    {
        if (IsFirst)
            return await _select(_operationResult.Value);

        if (IsAfterSync)
            return await _select(_previousSync.Execute());

        var previous = await _previous.ExecuteAsync();
        return await _select(previous);
    }

    bool IOperationExecutorAsync<TOut>.CanExecuted()
    {
        if (IsFirst)
            return _operationResult.Success;

        if (IsAfterSync)
            return _previousSync.CanExecuted();

        return _previous.CanExecuted();
    }

    OperationResult IOperationExecutorAsync<TOut>.GetInitialOperationResult()
    {
        if (IsFirst)
            return _operationResult;

        if (IsAfterSync)
            return _previousSync.GetInitialOperationResult();

        return _previous.GetInitialOperationResult();
    }
}