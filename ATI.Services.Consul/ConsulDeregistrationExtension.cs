using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ATI.Services.Consul
{
    [PublicAPI]
    public static class ConsulDeregistrationExtension
    {
        private const string DeregistrationAddress = "_internal/consul/deregister";

        public static IEndpointConventionBuilder MapConsulDeregistration(this IEndpointRouteBuilder builder,
            string deregistrationAddress = null)
        {
            return builder.MapDelete(deregistrationAddress ?? DeregistrationAddress, DeregisterDelegate);
        }

        private static async Task DeregisterDelegate(HttpContext _)
        {
            await ConsulRegistrator.DeregisterInstanceAsync();
        }
    }
}