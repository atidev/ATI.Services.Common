using System.Threading.Tasks;

namespace ATI.Services.Common.Behaviors
{
    public interface ILazyEvaluateAsync<TOut>
    {
        public Task<TOut> EvaluateOrThrowAsync();
        public bool CanEvaluated();
        public OperationResult GetInitialOperationResult();
    }
}