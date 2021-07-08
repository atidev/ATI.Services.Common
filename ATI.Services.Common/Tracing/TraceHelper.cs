using System.Collections.Generic;

namespace ATI.Services.Common.Tracing
{
    public static class TraceHelper
    {
        public static Dictionary<string, string> GetRabbitMqTracingInfo(string connectionString,
            string routingKey,
            string queue = null, string exchange = null)
        {
            var traceInfo = new Dictionary<string, string>
            {
                {"rabbitMq_connection_string", connectionString},
                {"rabbitMq_routingKey", routingKey},
            };

            if (queue != null)
                traceInfo.Add("rabbitMq_queue", queue);

            if (exchange != null)
                traceInfo.Add("rabbitMq_exchange", exchange);

            return traceInfo;
        }

        public static Dictionary<string, string> GetSQLTracingInfo(string procedureName, string connectionString)
        {
            return new()
            {
                    {"SQL_connection_string", connectionString},
                    {"SQL_procedure_name", procedureName},
                };
        }

        public static Dictionary<string, string> GetHttpTracingInfo(string httpUrl, string method, string body)
        {
            return new()
            {
                    {"http_url", httpUrl},
                    {"http_method", method},
                    {"http_body", body}
                };
        }

        public static Dictionary<string, string> GetMongoTracingInfo(string collectionFullName)
        {
            var traceInfo = new Dictionary<string, string>
            {
                {"mongo_db_collection_namespace", collectionFullName},
            };

            return traceInfo;
        }

        public static Dictionary<string, string> GetRedisTracingInfo(string connectionString, string setKey)
        {
            return new()
            {
                    {"redis_connection_string", connectionString},
                    {"key_example", setKey ?? "null" }
                };

        }
    }
}