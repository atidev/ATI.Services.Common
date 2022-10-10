﻿using System;
using System.Collections.Generic;
using System.Text.Json;
using ATI.Services.Common.Serializers;
using ATI.Services.Common.Serializers.SystemTextJsonSerialization;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace ATI.Services.Common.Tracing
{
    [PublicAPI]
    public class TracedHttpClientConfig
    {
        public TracedHttpClientConfig(
            string serviceName,
            TimeSpan timeout,
            SerializerType serializerType,
            bool addCultureToRequest = true,
            JsonSerializerSettings newtonsoftSettings = null,
            JsonSerializerOptions systemTextJsonOptions = null)
        {
            ServiceName = serviceName;
            Timeout = timeout;
            AddCultureToRequest = addCultureToRequest;
            SetSerializer(serializerType, newtonsoftSettings, systemTextJsonOptions);
        }

        public ISerializer Serializer { get; set; }
        public string ServiceName { get; init; }
        public TimeSpan Timeout { get; init; }
        public bool AddCultureToRequest { get; init; }
        
        public Dictionary<string, string> Headers { get; set; } = new();
        public List<string> HeadersToProxy { get; set; } = new();

        private void SetSerializer(
            SerializerType serializerType,
            JsonSerializerSettings newtonsoftSettings = null,
            JsonSerializerOptions systemTextJsonOptions = null)
        {
            Serializer = SerializerFactory.GetSerializerByType(serializerType);
            if (serializerType == SerializerType.Newtonsoft)
            {
                if (newtonsoftSettings != null)
                    Serializer.SetSerializeSettings(newtonsoftSettings);
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
                    Serializer.SetSerializeSettings(systemTextJsonOptions);
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