using System;

namespace ATI.Services.RabbitMQ
{
    public class EventbusOptions 
    {
        public string ServiceName { get; set; }
        public string ConnectionString { get; set; }
        public string Environment { get; set; }
        public TimeSpan RabbitConnectInterval { get; set; } = TimeSpan.FromSeconds(5);
        public bool Enabled { get; set; }
        public string ErrorQueueName { get; set; }

        #region ForLocalTesting

        public bool AddHostnamePostfixToQueues { get; set; }
        public bool DeleteQueuesOnApplicationShutdown { get; set; }

        #endregion
    }
}