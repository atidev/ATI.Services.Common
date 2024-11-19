using System.Net;
using ATI.Services.Common.Behaviors;

namespace ATI.Services.Common.Http.Extensions;

public static class OperationResultExtensions
{
    public static ActionStatus GetActionStatusByHttpStatusCode(HttpStatusCode httpStatusCode)
    {
        return httpStatusCode switch
        {
            _ when (int)httpStatusCode >= 200 && (int)httpStatusCode < 300 => ActionStatus.Ok,
            HttpStatusCode.BadRequest => ActionStatus.BadRequest,
            HttpStatusCode.Unauthorized => ActionStatus.Unauthorized,
            HttpStatusCode.PaymentRequired => ActionStatus.PaymentRequired,
            HttpStatusCode.Forbidden => ActionStatus.Forbidden,
            HttpStatusCode.NotFound => ActionStatus.NotFound,
            HttpStatusCode.RequestTimeout => ActionStatus.Timeout,
            HttpStatusCode.TooManyRequests => ActionStatus.TooManyRequests,
            HttpStatusCode.NotModified => ActionStatus.NotModified,
            _ => ActionStatus.InternalServerError
        };
    }

    public static OperationResult<T> ToOperationResult<T>(this HttpStatusCode httpStatusCode)
    {
        return new OperationResult<T>(GetActionStatusByHttpStatusCode(httpStatusCode));
    }

    public static OperationResult<T> ToOperationResult<T>(this HttpStatusCode httpStatusCode, T value)
    {
        return new OperationResult<T>(value, GetActionStatusByHttpStatusCode(httpStatusCode));
    }
}