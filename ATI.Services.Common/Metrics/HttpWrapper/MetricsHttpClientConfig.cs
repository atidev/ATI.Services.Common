using System;
using System.Collections.Generic;
using System.Text.Json;
using ATI.Services.Common.Serializers;
using ATI.Services.Common.Serializers.SystemTextJsonSerialization;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog;

namespace ATI.Services.Common.Metrics.HttpWrapper
{
    [PublicAPI]
    public class MetricsHttpClientConfig
    {
        public MetricsHttpClientConfig(
            string serviceName,
            TimeSpan timeout,
            SerializerType serializerType,
            bool addCultureToRequest = true,
            JsonSerializerSettings newtonsoftSettings = null,
            JsonSerializerOptions systemTextJsonOptions = null,
            bool propagateActivity = true,
            bool useHttpClientFactory = false)
        {
            ServiceName = serviceName;
            Timeout = timeout;
            AddCultureToRequest = addCultureToRequest;
            PropagateActivity = propagateActivity;
            UseHttpClientFactory = useHttpClientFactory;
            SetSerializer(serializerType, newtonsoftSettings, systemTextJsonOptions);
        }

        public ISerializer Serializer { get; set; }
        public string ServiceName { get; init; }
        public TimeSpan Timeout { get; init; }
        public bool AddCultureToRequest { get; init; }

        public Dictionary<string, string> Headers { get; set; } = new();
        public List<string> HeadersToProxy { get; set; } = new();
        public bool PropagateActivity { get; set; }
        
        public bool UseHttpClientFactory { get; set; }

        public Func<LogLevel, LogLevel> LogLevelOverride { get; set; } = level => level;

        public void SetSerializer(
            SerializerType serializerType,
            JsonSerializerSettings newtonsoftSettings = null,
            JsonSerializerOptions systemTextJsonOptions = null)
        {
            Serializer = SerializerFactory.GetSerializerByType(serializerType);
            if (serializerType == SerializerType.Newtonsoft)
            {
                if (newtonsoftSettings != null)
                {
                    Serializer.SetSerializeSettings(newtonsoftSettings);
                }
                else
                {
                    var jsonSerializerSettings = new JsonSerializerSettings()
                    {
                        ContractResolver = new DefaultContractResolver
                        {
                            NamingStrategy = new SnakeCaseNamingStrategy
                            {
                                ProcessDictionaryKeys = true
                            }
                        }
                    };
                    Serializer.SetSerializeSettings(jsonSerializerSettings);
                }
            }
            else
            {
                if (systemTextJsonOptions != null)
                {
                    Serializer.SetSerializeSettings(systemTextJsonOptions);
                }
                else
                {
                    var jsonSerializerSettings = new JsonSerializerOptions()
                    {
                        PropertyNamingPolicy = new SnakeCaseNamingPolicy()
                    };
                    Serializer.SetSerializeSettings(jsonSerializerSettings);
                }
            }
        }
    }
}