using System;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace ATI.Services.Common.Behaviors
{
    public class OperationResultAsyncSelector<TInternal, TOut> : ILazyEvaluateAsync<TOut>
    {
        [CanBeNull] private readonly ILazyEvaluate<TInternal> _previousSync;
        [CanBeNull] private readonly ILazyEvaluateAsync<TInternal> _previous;
        [CanBeNull] private readonly OperationResult<TInternal> _operationResult;
        private readonly Func<TInternal, Task<TOut>> _select;
        private bool IsFirst => _operationResult is not null;
        private bool IsAfterSync => _previousSync is not null;

        public OperationResultAsyncSelector([NotNull] ILazyEvaluate<TInternal> previousSync, Func<TInternal, Task<TOut>> select)
        {
            _previousSync = previousSync;
            _select = select;
            _operationResult = null;
            _previous = null;
        }
        
        public OperationResultAsyncSelector([NotNull] OperationResult<TInternal> operationResult, Func<TInternal, Task<TOut>> select)
        {
            _operationResult = operationResult;
            _select = select;
            _previous = null;
            _previousSync = null;
        }

        public OperationResultAsyncSelector(ILazyEvaluateAsync<TInternal> previous, Func<TInternal, Task<TOut>> select)
        {
            _select = select;
            _previous = previous;
            _operationResult = null;
            _previousSync = null;
        }

        public async Task<TOut> EvaluateOrThrowAsync()
        {
            if (IsFirst)
                return await _select(_operationResult.Value);

            if (IsAfterSync)
                return await _select(_previousSync.EvaluateOrThrow());

            var previous = await _previous.EvaluateOrThrowAsync();
            return await _select(previous);
        }

        public bool CanEvaluated()
        {
            if (IsFirst)
                return _operationResult.Success;

            if (IsAfterSync)
                return _previousSync.CanEvaluated();

            return _previous.CanEvaluated();
        }

        public OperationResult GetInitialOperationResult()
        {
            if (IsFirst)
                return _operationResult;

            if (IsAfterSync)
                return _previousSync.GetInitialOperationResult();

            return _previous.GetInitialOperationResult();
        }
    }
}