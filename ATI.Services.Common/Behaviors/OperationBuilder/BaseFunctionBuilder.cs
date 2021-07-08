using System;
using System.Threading.Tasks;


namespace ATI.Services.Common.Behaviors.OperationBuilder
{
    public abstract class BaseFunctionBuilder<T> : BaseActionBuilder
    {
        internal Task<OperationResult<T>> FunctionTask { get; set; }
        internal OperationResult<T> FunctionResult { get; set; }
        protected internal Func<T, bool> NotFoundCondition { protected get; set; }
        protected internal bool NullNotFoundCondition { get; set; }
        protected internal bool? ShouldSerializePrivateProperties { get; set; }

    }
}
