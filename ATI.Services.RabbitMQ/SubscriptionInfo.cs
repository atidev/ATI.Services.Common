using System;
using System.Threading.Tasks;
using EasyNetQ;

namespace ATI.Services.RabbitMQ
{
    public class SubscriptionInfo
    {
        public QueueExchangeBinding Binding { get; set; }
        public bool Durable { get; set; }
        public bool AutoDelete { get; set; }
        public Func<byte[], MessageProperties, MessageReceivedInfo, Task> EventbusSubscriptionHandler { get; set; }

        public string MetricsEntity { get; set; }
    }
}