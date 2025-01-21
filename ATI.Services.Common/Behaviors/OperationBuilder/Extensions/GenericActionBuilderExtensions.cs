using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;
#nullable enable

namespace ATI.Services.Common.Behaviors.OperationBuilder.Extensions;

[PublicAPI]
public static class GenericActionBuilderExtensions
{
    public static GenericActionBuilder<T> AsActionBuilder<T>(this Task<OperationResult<T>> operation)
    {
        return new(operation);
    }
        
    public static GenericActionBuilder<T> AsActionBuilder<T>(this OperationResult<T> operation)
    {
        return new(operation);
    }
        
    public static async Task<IActionResult> AsActionResultAsync<T>(this Task<OperationResult<T>> operationTask, bool? internalFlag = false)
    {
        var actionBuilder = new GenericActionBuilder<T>(operationTask);
        if (internalFlag.HasValue)
        {
            actionBuilder.WithInternalFlag(internalFlag.Value);
        }
        return await actionBuilder.ExecuteAsync();
    }
        
    public static IActionResult AsActionResult<T>(this OperationResult<T> operation, bool? internalFlag = false)
    {
        var actionBuilder = new GenericActionBuilder<T>(operation);
        if (internalFlag.HasValue)
        {
            actionBuilder.WithInternalFlag(internalFlag.Value);
        }
        return actionBuilder.Execute();
    }
        
    public static GenericActionBuilder<T> WithActionResultRewrite<T>(this GenericActionBuilder<T> builder, 
        Dictionary<ActionStatus, Func<T?, IActionResult>> resultRewrites)
    {
        builder.ActionResultRewrite = resultRewrites;
        return builder;
    }
        
    public static GenericActionBuilder<T> WithNotFoundCondition<T>(this GenericActionBuilder<T> function, Func<T?, bool> notFoundCondition)
    {
        function.NotFoundCondition = notFoundCondition;
        return function;
    }
        
    public static GenericActionBuilder<T> WithNullNotFoundCondition<T>(this GenericActionBuilder<T> function)
    {
        function.NullNotFoundCondition = true;
        return function;
    }
        
    public static GenericActionBuilder<T> WithShouldSerializePrivateProperties<T>(this GenericActionBuilder<T> function, bool shouldSerialize)
        where T : IPrivatePropertiesContainer
    {
        function.ShouldSerializePrivateProperties = shouldSerialize;
        return function;
    }
}