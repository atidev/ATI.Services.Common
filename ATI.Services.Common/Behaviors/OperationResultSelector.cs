#nullable enable
using System;

namespace ATI.Services.Common.Behaviors;

public class OperationResultSelector<TInternal, TOut> : IOperationExecutor<TOut>
{
    private readonly IOperationExecutor<TInternal>? _previous;
    private readonly OperationResult<TInternal>? _operationResult;
    private readonly Func<TInternal, TOut> _select;
    private bool IsFirst => _previous is null;

    public OperationResultSelector(OperationResult<TInternal> operationResult, Func<TInternal, TOut> select)
    {
        _operationResult = operationResult;
        _select = select;
        _previous = null;
    }
        
    public OperationResultSelector(IOperationExecutor<TInternal> previous, Func<TInternal, TOut> select)
    {
        _operationResult = null;
        _select = select;
        _previous = previous;
    }

    TOut IOperationExecutor<TOut>.Evaluate()
    {
        if (IsFirst)
            return _select(_operationResult.Value);

        return _select(_previous.Evaluate());
    }

    bool IOperationExecutor<TOut>.CanEvaluated()
    {
        if (IsFirst)
            return _operationResult.Success;

        return _previous.CanEvaluated();
    }

    OperationResult IOperationExecutor<TOut>.GetInitialOperationResult()
    {
        if (IsFirst)
            return _operationResult;

        return _previous.GetInitialOperationResult();
    }
}