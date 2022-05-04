using System;
using JetBrains.Annotations;

namespace ATI.Services.Common.Behaviors
{
    public class OperationResultSelector<TInternal, TOut> : ILazyEvaluate<TOut>
    {
        [CanBeNull] private readonly ILazyEvaluate<TInternal> _previous;
        [CanBeNull] private readonly OperationResult<TInternal> _operationResult;
        private readonly Func<TInternal, TOut> _select;
        private bool IsFirst => _previous is null;

        public OperationResultSelector([NotNull] OperationResult<TInternal> operationResult, Func<TInternal, TOut> select)
        {
            _operationResult = operationResult;
            _select = select;
            _previous = null;
        }
        
        public OperationResultSelector(ILazyEvaluate<TInternal> previous, Func<TInternal, TOut> select)
        {
            _operationResult = null;
            _select = select;
            _previous = previous;
        }

        public TOut EvaluateOrThrow()
        {
            if (IsFirst)
                return _select(_operationResult.Value);

            return _select(_previous.EvaluateOrThrow());
        }

        public bool CanEvaluated()
        {
            if (IsFirst)
                return _operationResult.Success;

            return _previous.CanEvaluated();
        }

        public OperationResult GetInitialOperationResult()
        {
            if (IsFirst)
                return _operationResult;

            return _previous.GetInitialOperationResult();
        }
    }
}