namespace ATI.Services.Common.Behaviors
{
    public interface ILazyEvaluate<out TOut>
    {
        public TOut EvaluateOrThrow();
        public bool CanEvaluated();
        public OperationResult GetInitialOperationResult();
    }
}