namespace ATI.Services.Common.Behaviors;

public interface IOperationExecutor<out TOut>
{
    internal TOut Evaluate();
    internal bool CanEvaluated();
    internal OperationResult GetInitialOperationResult();
}