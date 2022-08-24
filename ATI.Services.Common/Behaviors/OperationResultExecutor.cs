#nullable enable
using System;

namespace ATI.Services.Common.Behaviors;

public sealed class OperationResultExecutor<TSource, TOut> : IOperationExecutor<TOut>
{
    private readonly IOperationExecutor<TSource>? _previous;
    private readonly OperationResult<TSource>? _operationResult;
    private readonly Func<TSource, TOut> _select;
    private bool IsFirst => _previous is null;

    public OperationResultExecutor(OperationResult<TSource> operationResult, Func<TSource, TOut> select)
    {
        _operationResult = operationResult;
        _select = select;
        _previous = null;
    }
        
    public OperationResultExecutor(IOperationExecutor<TSource> previous, Func<TSource, TOut> select)
    {
        _operationResult = null;
        _select = select;
        _previous = previous;
    }

    TOut IOperationExecutor<TOut>.Execute()
    {
        if (IsFirst)
            return _select(_operationResult.Value);

        return _select(_previous.Execute());
    }

    bool IOperationExecutor<TOut>.CanExecuted()
    {
        if (IsFirst)
            return _operationResult.Success;

        return _previous.CanExecuted();
    }

    OperationResult IOperationExecutor<TOut>.GetInitialOperationResult()
    {
        if (IsFirst)
            return _operationResult;

        return _previous.GetInitialOperationResult();
    }
}