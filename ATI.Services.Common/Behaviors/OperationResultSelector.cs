#nullable enable
using System;

namespace ATI.Services.Common.Behaviors;

public class OperationResultSelector<TSource, TOut> : IOperationExecutor<TOut>
{
    private readonly IOperationExecutor<TSource>? _previous;
    private readonly OperationResult<TSource>? _operationResult;
    private readonly Func<TSource, TOut> _select;
    private bool IsFirst => _previous is null;

    public OperationResultSelector(OperationResult<TSource> operationResult, Func<TSource, TOut> select)
    {
        _operationResult = operationResult;
        _select = select;
        _previous = null;
    }
        
    public OperationResultSelector(IOperationExecutor<TSource> previous, Func<TSource, TOut> select)
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