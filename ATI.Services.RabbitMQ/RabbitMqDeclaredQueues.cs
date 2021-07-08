using System.Collections.Generic;
using EasyNetQ.Topology;

namespace ATI.Services.RabbitMQ
{
    public static class RabbitMqDeclaredQueues
    {
        public static List<IQueue> DeclaredQueues { get; } = new();
    }
}