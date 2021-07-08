using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ATI.Services.Common.Extensions;
using ATI.Services.Common.Initializers;
using ATI.Services.Common.Initializers.Interfaces;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Metrics;
using EasyNetQ;
using EasyNetQ.Topology;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;
using NLog;
using Polly;
using Polly.Wrap;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

namespace ATI.Services.RabbitMQ
{
    [PublicAPI]
    [InitializeOrder(Order = InitializeOrder.First)]
    public class EventbusManager : IDisposable, IInitializer
    {
        private IAdvancedBus _busClient;
        private const int RetryAttemptMax = 4;
        private readonly JsonSerializer _jsonSerializer;
        private readonly string _connectionString;

        private readonly MetricsTracingFactory _metricsTracingFactory =
            MetricsTracingFactory.CreateRepositoryMetricsFactory(nameof(EventbusManager));

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private List<SubscriptionInfo> _exclusiveSubscriptions = new();
        private readonly Policy _retryForeverPolicy;
        private readonly Policy _subscribePolicy;
        private readonly EventbusOptions _options;
        private static readonly UTF8Encoding BodyEncoding = new(false);

        public EventbusManager(JsonSerializer jsonSerializer,
            IOptions<EventbusOptions> options)
        {
            _options = options.Value;
            _connectionString = options.Value.ConnectionString;
            _jsonSerializer = jsonSerializer;

            _subscribePolicy = Policy.Handle<Exception>()
                .WaitAndRetryForeverAsync(
                    _ => _options.RabbitConnectInterval,
                    (exception, _) => _logger.Error(exception));

            _retryForeverPolicy =
                Policy.Handle<Exception>()
                    .WaitAndRetryForeverAsync(
                        retryAttempt =>
                            retryAttempt > RetryAttemptMax
                                ? TimeSpan.FromSeconds(2)
                                : TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                        (exception, _) => _logger.ErrorWithObject(exception, _exclusiveSubscriptions));
        }

        private async Task ResubscribeOnReconnect()
        {
            try
            {
                foreach (var subscription in _exclusiveSubscriptions)
                {
                    await _retryForeverPolicy.ExecuteAsync(async () => await SubscribePrivateAsync(
                        subscription.Binding,
                        subscription.Durable,
                        subscription.AutoDelete,
                        subscription.EventbusSubscriptionHandler,
                        subscription.MetricsEntity));
                }
            }
            catch (Exception e)
            {
                _logger.ErrorWithObject(e, _exclusiveSubscriptions);
            }
        }

        public Task InitializeAsync()
        {
            try
            {
                _busClient = RabbitHutch.CreateBus(_connectionString,
                    serviceRegister =>
                    {
                        serviceRegister.Register<IConventions>(c =>
                            new RabbitMqConventions(c.Resolve<ITypeNameSerializer>(), _options));
                    }).Advanced;

                _busClient.Connected += async (_, _) => await ResubscribeOnReconnect();
                _busClient.Disconnected += (_, _) => { _logger.Error("Disconnected from RMQ for some reason!"); };
            }
            catch (Exception exception)
            {
                _logger.Error(exception);
            }
            return Task.CompletedTask;
        }

        public Task<IExchange> DeclareExchangeTopicAsync(string exchangeName)
        {
            return _busClient.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic);
        }

        public Task<IExchange[]> DeclareExchangeTopicAsync(params string[] exchangeNames)
        {
            var tasks = exchangeNames.Select(exchangeName =>
                _busClient.ExchangeDeclareAsync(exchangeName, ExchangeType.Topic));
            return Task.WhenAll(tasks);
        }

        public async Task PublishRawAsync(
            string publishBody,
            string exchangeName,
            string routingKey,
            string metricEntity,
            bool mandatory = false,
            TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(exchangeName) || string.IsNullOrWhiteSpace(routingKey) ||
                string.IsNullOrWhiteSpace(publishBody))
                return;

            using (_metricsTracingFactory.CreateLoggingMetricsTimer(metricEntity))
            {
                var messageProperties = new MessageProperties();
                var exchange = new Exchange(exchangeName);
                var body = BodyEncoding.GetBytes(publishBody);

                var sendingResult = await SetupPolicy(timeout).ExecuteAndCaptureAsync(async () =>
                    await _busClient.PublishAsync(
                        exchange,
                        routingKey,
                        mandatory,
                        messageProperties,
                        body));

                if (sendingResult.FinalException != null)
                {
                    _logger.ErrorWithObject(sendingResult.FinalException,
                        new {publishBody, exchangeName, routingKey, metricEntity, mandatory});
                }
            }
        }

        public async Task PublishAsync<T>(
            T publishObject,
            string exchangeName,
            string routingKey,
            string metricEntity,
            bool mandatory = false,
            JsonSerializer serializer = null,
            TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(exchangeName) || string.IsNullOrWhiteSpace(routingKey) ||
                publishObject == null)
                return;

            using (_metricsTracingFactory.CreateLoggingMetricsTimer(metricEntity))
            {
                var messageProperties = new MessageProperties();
                var exchange = new Exchange(exchangeName);
                var bodySerializer = serializer ?? _jsonSerializer;
                var body = bodySerializer.ToJsonBytes(publishObject);

                var sendingResult = await SetupPolicy(timeout).ExecuteAndCaptureAsync(async () =>
                    await _busClient.PublishAsync(
                        exchange,
                        routingKey,
                        mandatory,
                        messageProperties,
                        body));

                if (sendingResult.FinalException != null)
                {
                    _logger.ErrorWithObject(sendingResult.FinalException,
                        new {publishObject, exchangeName, routingKey, metricEntity, mandatory});
                }
            }
        }

        private async Task SubscribePrivateAsync(
            QueueExchangeBinding bindingInfo,
            bool durable,
            bool autoDelete,
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> handler,
            string metricEntity)
        {
            var exchange = await _busClient.ExchangeDeclareAsync(bindingInfo.Exchange.Name, bindingInfo.Exchange.Type);
            var queue = await _busClient.QueueDeclareAsync(bindingInfo.Queue.Name, autoDelete: autoDelete,
                durable: durable,
                exclusive: bindingInfo.Queue.IsExclusive);
            _busClient.Bind(exchange, bindingInfo.Queue, bindingInfo.RoutingKey);
            _busClient.Consume(queue,
                async (body, props, info) =>
                    await HandleEventBusMessageWithPolicy(body, props, info));

            async Task HandleEventBusMessageWithPolicy(byte[] body, MessageProperties props,
                MessageReceivedInfo info)
            {
                using (_metricsTracingFactory.CreateLoggingMetricsTimer(metricEntity ?? "Eventbus"))
                    await ExecuteWithPolicy(async () => await handler.Invoke(body, props, info));
            }
        }

        public async Task SubscribeAsync(
            QueueExchangeBinding bindingInfo,
            bool durable,
            bool autoDelete,
            Func<byte[], MessageProperties, MessageReceivedInfo, Task> handler,
            string metricEntity = null)
        {
            if (bindingInfo.Queue.IsExclusive)
            {
                _exclusiveSubscriptions.Add(new SubscriptionInfo
                {
                    Binding = bindingInfo,
                    Durable = durable,
                    AutoDelete = autoDelete,
                    EventbusSubscriptionHandler = handler,
                    MetricsEntity = metricEntity
                });
            }

            RabbitMqDeclaredQueues.DeclaredQueues.Add(bindingInfo.Queue);

            if (_busClient.IsConnected)
            {
                try
                {
                    await SubscribePrivateAsync(bindingInfo, durable, autoDelete, handler, metricEntity);
                }
                // В интервале между проверкой _busClient.IsConnected и SubscribeAsyncPrivate Rabbit может отвалиться, поэтому запускаем в бекграунд потоке
                catch (Exception ex)
                {
                    _logger.Error(ex);
                    _subscribePolicy.ExecuteAsync(async () =>
                        await SubscribePrivateAsync(bindingInfo, durable, autoDelete, handler, metricEntity)).Forget();
                }
            }
            else
            {
                _subscribePolicy.ExecuteAsync(async () =>
                    await SubscribePrivateAsync(bindingInfo, durable, autoDelete, handler, metricEntity)).Forget();
            }
        }


        private async Task ExecuteWithPolicy(Func<Task> action)
        {
            var policy = Policy.Handle<TimeoutException>()
                .WaitAndRetryAsync(
                    RetryAttemptMax,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, _) =>
                    {
                        _logger.ErrorWithObject(exception, new {TimeSpan = timeSpan, RetryCount = retryCount});
                    });

            var policyResult = await policy.ExecuteAndCaptureAsync(async () => await action.Invoke());

            if (policyResult.FinalException != null)
            {
                _logger.ErrorWithObject(policyResult.FinalException, action);
            }
        }

        private PolicyWrap SetupPolicy(TimeSpan? timeout = null) =>
            Policy.WrapAsync(Policy.TimeoutAsync(timeout ?? TimeSpan.FromSeconds(2)),
                Policy.Handle<Exception>()
                    .WaitAndRetryAsync(3, _ => TimeSpan.FromSeconds(3)));

        public void Dispose()
        {
            // Сделано для удобства локального тестирования, удаляем наши созданные очереди
            if (_options.DeleteQueuesOnApplicationShutdown && _busClient != null)
            {
                foreach (var queue in RabbitMqDeclaredQueues.DeclaredQueues)
                {
                    _busClient.QueueDelete(queue);
                }
            }

            _busClient?.Dispose();
        }
    }
}