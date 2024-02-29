using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;

namespace ATI.Services.Common.Behaviors.OperationBuilder.Extensions
{
    [PublicAPI]
    public static class ConvertibleActionBuilderExtensions
    {
        public static ConvertibleActionBuilder<T, TOut> AsActionBuilder<T, TOut>(this Task<OperationResult<T>> operationTask, Func<T, TOut> resultConvertFunc)
        {
            return new(operationTask, resultConvertFunc);
        }
        
        public static ConvertibleActionBuilder<T, TOut> AsActionBuilder<T, TOut>(this OperationResult<T> operation, Func<T, TOut> resultConvertFunc)
        {
            return new(operation, resultConvertFunc);
        }
        
        public static async Task<IActionResult> AsActionResultAsync<T, TOut>(this Task<OperationResult<T>> operationTask, Func<T, TOut> resultConvertFunc)
        {
            return await new ConvertibleActionBuilder<T, TOut>(operationTask, resultConvertFunc).ExecuteAsync();
        }
        
        public static IActionResult AsActionResult<T, TOut>(this OperationResult<T> operation, Func<T, TOut> resultConvertFunc)
        {
            return new ConvertibleActionBuilder<T, TOut>(operation, resultConvertFunc).Execute();
        }
        
        public static ConvertibleActionBuilder<T, TOut> WithActionResultRewrite<T, TOut>(
            this ConvertibleActionBuilder<T, TOut> builder, 
            Dictionary<ActionStatus, Func<TOut, IActionResult>> resultRewrites)
        {
            builder.ActionResultRewrite = resultRewrites;
            return builder;
        }
        
        public static ConvertibleActionBuilder<TIn, TRes> WithNotFoundCondition<TIn, TRes>(
            this ConvertibleActionBuilder<TIn, TRes> function, 
            Func<TIn, bool> notFoundCondition,
            Func<TRes, bool> notFoundConditionAfterDataConvert = null)
        {
            function.NotFoundCondition = notFoundCondition;
            if (notFoundConditionAfterDataConvert != null)
            {
                function.WithNotFoundConditionAfterDataConvert(notFoundConditionAfterDataConvert);
            }
            
            return function;
        }
        
        public static ConvertibleActionBuilder<TIn, TRes> WithNotFoundConditionAfterDataConvert<TIn, TRes>(
            this ConvertibleActionBuilder<TIn, TRes> function, Func<TRes, bool> notFoundCondition)
        {
            function.NotFoundConditionAfterDataConvert = notFoundCondition;
            return function;
        }
        
        public static ConvertibleActionBuilder<TIn, TRes> WithNullNotFoundCondition<TIn, TRes>(
            this ConvertibleActionBuilder<TIn, TRes> function, bool? withNullNotFoundConditionAfterDataConvert = null)
        {
            function.NullNotFoundCondition = true;
            if (withNullNotFoundConditionAfterDataConvert.HasValue && withNullNotFoundConditionAfterDataConvert.Value)
            {
                function.WithNullNotFoundConditionAfterDataConvert();
            }
            
            return function;
        }
        
        public static ConvertibleActionBuilder<TIn, TRes> WithNullNotFoundConditionAfterDataConvert<TIn, TRes>(
            this ConvertibleActionBuilder<TIn, TRes> function)
        {
            function.NullNotFoundConditionAfterDataConvert = true;
            return function;
        }
        
        public static ConvertibleActionBuilder<TIn, TRes> WithResultConvert<TIn, TRes>(this GenericActionBuilder<TIn> function, 
            Func<TIn, TRes> resultConvertFun)
        {
            return function.FunctionTask != null
                ? new ConvertibleActionBuilder<TIn, TRes>(function.FunctionTask, resultConvertFun)
                : new ConvertibleActionBuilder<TIn, TRes>(function.FunctionResult, resultConvertFun);
        }
        
        public static ConvertibleActionBuilder<TIn, TRes> WithShouldSerializePrivateProperties<TIn, TRes>(
            this ConvertibleActionBuilder<TIn, TRes> function, bool shouldSerialize) where TRes : IPrivatePropertiesContainer
        {
            function.ShouldSerializePrivateProperties = shouldSerialize;
            return function;
        }
    }
}