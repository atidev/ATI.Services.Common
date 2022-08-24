namespace ATI.Services.Common.Behaviors;

public interface IOperationExecutor<out TOut>
{
    internal TOut Execute();
    internal bool CanExecuted();
    internal OperationResult GetInitialOperationResult();
}