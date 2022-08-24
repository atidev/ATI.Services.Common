using System.Threading.Tasks;

namespace ATI.Services.Common.Behaviors;

public interface IOperationExecutorAsync<TOut>
{
    internal Task<TOut> ExecuteAsync();
    internal bool CanEvaluated();
    internal OperationResult GetInitialOperationResult();
}