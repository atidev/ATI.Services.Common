using System.Collections.Generic;
using EasyNetQ.Topology;

namespace ATI.Services.RabbitMQ
{
    public class QueueExchangeBinding
    {
        public QueueExchangeBinding(ExchangeInfo exchange, IQueue queue, string routingKey)
        {
            Queue = queue;
            RoutingKey = routingKey;
            Exchange = exchange;
            List<decimal> d = new List<decimal>();
        }

        public IQueue Queue { get; }
        public string RoutingKey { get; }
        public ExchangeInfo Exchange { get; }
    }
}
