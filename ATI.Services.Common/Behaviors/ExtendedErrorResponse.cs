using System.Net;
using JetBrains.Annotations;

namespace ATI.Services.Common.Behaviors
{
    public class ExtendedErrorResponse : ErrorResponse
    {
        public HttpStatusCode StatusCode { get; [PublicAPI] set; }
    }
}