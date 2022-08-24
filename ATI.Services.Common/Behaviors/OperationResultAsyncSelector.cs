#nullable enable
using System;
using System.Threading.Tasks;

namespace ATI.Services.Common.Behaviors;

public class OperationResultAsyncSelector<TInternal, TOut> : IOperationExecutorAsync<TOut>
{
    private readonly IOperationExecutor<TInternal>? _previousSync;
    private readonly IOperationExecutorAsync<TInternal>? _previous;
    private readonly OperationResult<TInternal>? _operationResult;
    private readonly Func<TInternal, Task<TOut>> _select;
    private bool IsFirst => _operationResult is not null;
    private bool IsAfterSync => _previousSync is not null;

    public OperationResultAsyncSelector(IOperationExecutor<TInternal> previousSync, Func<TInternal, Task<TOut>> select)
    {
        _previousSync = previousSync;
        _select = select;
        _operationResult = null;
        _previous = null;
    }
        
    public OperationResultAsyncSelector(OperationResult<TInternal> operationResult, Func<TInternal, Task<TOut>> select)
    {
        _operationResult = operationResult;
        _select = select;
        _previous = null;
        _previousSync = null;
    }

    public OperationResultAsyncSelector(IOperationExecutorAsync<TInternal> previous, Func<TInternal, Task<TOut>> select)
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
            return await _select(_previousSync.Evaluate());

        var previous = await _previous.ExecuteAsync();
        return await _select(previous);
    }

    bool IOperationExecutorAsync<TOut>.CanEvaluated()
    {
        if (IsFirst)
            return _operationResult.Success;

        if (IsAfterSync)
            return _previousSync.CanEvaluated();

        return _previous.CanEvaluated();
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