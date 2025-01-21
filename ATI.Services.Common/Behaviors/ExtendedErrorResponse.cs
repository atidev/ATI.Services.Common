using System.Net;
using JetBrains.Annotations;
#nullable enable

namespace ATI.Services.Common.Behaviors;

public class ExtendedErrorResponse : ErrorResponse
{
    public HttpStatusCode StatusCode { get; [PublicAPI] set; }
}