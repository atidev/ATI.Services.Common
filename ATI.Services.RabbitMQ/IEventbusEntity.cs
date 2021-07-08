using JetBrains.Annotations;

namespace ATI.Services.RabbitMQ
{
    [PublicAPI]
    public interface IEventbusEntity
    {
        string GetRoutingKey(string action);
    }
}
