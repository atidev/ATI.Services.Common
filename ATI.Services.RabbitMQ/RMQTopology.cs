using System.Net;
using ATI.Services.Common.Behaviors;
using EasyNetQ.Topology;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace ATI.Services.RabbitMQ
{
    [PublicAPI]
    public class RmqTopology
    {
        private readonly EventbusOptions _eventbusOptions;
        private readonly string _queuePostfixName= "-" + Dns.GetHostName() + "-" + ConfigurationManager.GetApplicationPort();
        private const string SubscriptionType = "eventbus";
        
        public RmqTopology(IOptions<EventbusOptions> options)
        {
            _eventbusOptions = options.Value;
        }

        /// <summary>
        /// </summary>
        /// <param name="rabbitService"></param>
        /// <param name="routingKey"></param>
        /// <param name="operation"></param>
        /// <param name="isExclusiveQueueName">Если true, то к имени очереди добавится постфикс с именем машины+порт</param>
        /// <param name="isExclusive"></param>
        /// <param name="customQueueName"></param>
        /// <returns></returns>
        public QueueExchangeBinding CreateBinding(
            string rabbitService,
            string routingKey,
            string operation = Operation.Any,
            bool isExclusiveQueueName = false,
            bool isExclusive = false,
            string customQueueName = null)
        {
           var queueName = isExclusiveQueueName 
                ? GetEventbusExclusiveQueueName(rabbitService, routingKey, operation, customQueueName) 
                : EventbusQueueNameTemplate(rabbitService, routingKey, operation, customQueueName);

            var subscribeExchange = new ExchangeInfo
            {
                Name = $"{_eventbusOptions.Environment}.{rabbitService}",
                Type = ExchangeType.Topic
            };

            var createdQueue = new Queue(queueName, isExclusive);
            return new QueueExchangeBinding(subscribeExchange, createdQueue, routingKey);
        }
        
        private string GetEventbusExclusiveQueueName(string rabbitService, string exchange, string operation, string customQueueName)
        {
            return
                $"{_eventbusOptions.Environment}.{SubscriptionType}." +
                (string.IsNullOrEmpty(customQueueName)
                    ? $"{_eventbusOptions.ServiceName}.{rabbitService}.{exchange}.{operation}{_queuePostfixName}"
                    : $"{customQueueName}{_queuePostfixName}");
        }

        private string EventbusQueueNameTemplate(string rabbitService, string routingKey, string operation, string customQueueName)
        {
            var queueName = $"{_eventbusOptions.Environment}.{SubscriptionType}.";
            if (string.IsNullOrEmpty(customQueueName))
            {
                queueName += $"{_eventbusOptions.ServiceName}.{rabbitService}.{routingKey}.{operation}";
            }
            else
            {
                queueName += customQueueName;
            }
            
            if (_eventbusOptions.AddHostnamePostfixToQueues)
                queueName += _queuePostfixName;

            return queueName;
        }
        
    }
}
