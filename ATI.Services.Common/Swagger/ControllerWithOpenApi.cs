using System.Net;
using ATI.Services.Common.Behaviors;
using Microsoft.AspNetCore.Mvc;

namespace ATI.Services.Common.Swagger
{
    [Produces("application/json")]
    [SwaggerTag(SwaggerTag.All)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.InternalServerError)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.GatewayTimeout)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.PaymentRequired)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), (int)HttpStatusCode.NotFound)]
    public class ControllerWithOpenApi : ControllerBase
    {
    }
}
