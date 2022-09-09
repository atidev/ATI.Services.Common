using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;

namespace ATI.Services.Common.Extensions
{
    public static class TaskExtensions
    {
        public static void Forget(this Task task)
        {
        }

        public static IOperationExecutorAsync<OperationResult<TResult>> ToOperationExecutor<TResult>(this Task<OperationResult<TResult>> task)
        {
            return new OperationResultAsyncExecutor<int, OperationResult<TResult>>(new OperationResult<int>(OperationResult.Ok), _ => task);
        }
    }
}
