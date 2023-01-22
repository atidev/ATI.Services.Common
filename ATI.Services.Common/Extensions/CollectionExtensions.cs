using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using JetBrains.Annotations;
using Polly.Caching;

namespace ATI.Services.Common.Extensions;

[PublicAPI]
public static class OperationResultCollectionExtensions
{
    public static async void Add<T>(this ICollection<OperationResult> collection, Task<OperationResult<T>> task) 
        => collection.Add(await task);

    public static bool Success(this ICollection<OperationResult> collection) 
        => collection.All(i => i.Success);

    public static OperationResult GetNotSuccessOperation(this ICollection<OperationResult> collection) 
        => collection.FirstOrDefault(i => !i.Success);
}