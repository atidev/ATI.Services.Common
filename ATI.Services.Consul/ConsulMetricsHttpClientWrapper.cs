using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Metrics;
using ATI.Services.Common.Options;
using ATI.Services.Common.Tracing;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NLog;

namespace ATI.Services.Consul
{
    /// <summary>
    /// Обертка, включающая в себя ConsulServiceAddress, TracingHttpClientWrapper и MetricsTracingFactory
    /// </summary>
    [PublicAPI]
    public class ConsulMetricsHttpClientWrapper
    {
        private readonly TracingHttpClientWrapper _clientWrapper;
        private readonly MetricsTracingFactory _metricsTracingFactory;
        private readonly ConsulServiceAddress _serviceAddress;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public ConsulMetricsHttpClientWrapper(
            BaseServiceOptions serviceOptions,
            string adapterName,
            JsonSerializer serializer = null)
        {
            _metricsTracingFactory = MetricsTracingFactory.CreateHttpClientMetricsFactory(adapterName,
                serviceOptions.ConsulName, serviceOptions.LongRequestTime);

            _serviceAddress =
                new ConsulServiceAddress(serviceOptions.ConsulName, serviceOptions.Environment);

            var config = new TracedHttpClientConfig
            {
                ServiceName = serviceOptions.ConsulName,
                Timeout = serviceOptions.TimeOut
            };

            if (serviceOptions.AdditionalHeaders != null)
            {
                foreach (var header in serviceOptions.AdditionalHeaders)
                    config.Headers.TryAdd(header.Key, header.Value);
            }

            if (serializer != null)
                config.Serializer = serializer;

            _clientWrapper = new TracingHttpClientWrapper(config);
        }

        #region Get

        public Task<OperationResult<TResponse>> GetAsync<TResponse>(string url, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.GetAsync<TResponse>(serviceAddress, metricName, url, headers),
                additionalErrorLogObjects);
        }

        public Task<OperationResult<string>> GetAsync(string url, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.GetAsync(serviceAddress, metricName, url, headers), additionalErrorLogObjects);
        }

        #endregion

        #region Post

        public Task<OperationResult<TResponse>> PostAsync<TBody, TResponse>(string url, TBody body, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels, body,
                serviceAddress =>
                    _clientWrapper.PostAsync<TBody, TResponse>(serviceAddress, metricName, url, body, headers),
                additionalErrorLogObjects);
        }

        public Task<OperationResult<TResponse>> PostAsync<TResponse>(string url, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.PostAsync<TResponse>(serviceAddress, metricName, url, headers));
        }

        public Task<OperationResult<TResponse>> PostAsync<TResponse>(string url, string body, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels, body,
                serviceAddress =>
                    _clientWrapper.PostAsync<TResponse>(serviceAddress, metricName, url, body, headers));
        }

        public Task<OperationResult<string>> PostAsync(string url, string body, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels, body,
                serviceAddress =>
                    _clientWrapper.PostAsync(serviceAddress, metricName, url, body, headers));
        }

        public Task<OperationResult<string>> PostAsync<T>(string url, T body, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels, body,
                serviceAddress =>
                    _clientWrapper.PostAsync(serviceAddress, metricName, url, body, headers));
        }

        #endregion

        #region Put

        public Task<OperationResult<TResponse>> PutAsync<TBody, TResponse>(string url, TBody body, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels, body,
                serviceAddress =>
                    _clientWrapper.PutAsync<TBody, TResponse>(serviceAddress, metricName, url, body, headers));
        }

        public Task<OperationResult<TResponse>> PutAsync<TResponse>(string url, string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.PutAsync<TResponse>(serviceAddress, metricName, url, headers));
        }

        public Task<OperationResult<string>> PutAsync(
            string url, string metricName, Dictionary<string, string> headers = null,
            string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.PutAsync(serviceAddress, metricName, url, headers));
        }

        #endregion

        #region Delete

        public Task<OperationResult<TResponse>> DeleteAsync<TBody, TResponse>(
            string url,
            TBody body,
            string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels, body,
                serviceAddress =>
                    _clientWrapper.DeleteAsync<TBody, TResponse>(serviceAddress, metricName, url, body, headers));
        }

        public Task<OperationResult<TResponse>> DeleteAsync<TResponse>(string url,
            string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.DeleteAsync<TResponse>(serviceAddress, metricName, url, headers));
        }

        public Task<OperationResult<string>> DeleteAsync(string url,
            string metricName,
            Dictionary<string, string> headers = null, string urlTemplate = null, string[] additionalLabels = null,
            params object[] additionalErrorLogObjects)
        {
            return SendAsync(url, urlTemplate, metricName, headers, additionalLabels,
                serviceAddress =>
                    _clientWrapper.DeleteAsync(serviceAddress, metricName, url, headers));
        }

        #endregion

        private Task<OperationResult<T>> SendAsync<T>(string url,
            string urlTemplate,
            string metricName,
            Dictionary<string, string> headers,
            string[] additionalLabels,
            Func<string, Task<OperationResult<T>>> methodExecuteFunc,
            params object[] errorLogObjects)
        {
            using (_metricsTracingFactory.CreateLoggingMetricsTimer(metricName,
                $"{HttpMethod.Delete}:{urlTemplate ?? url}", additionalLabels))
            {
                try
                {
                    var serviceAddress = _serviceAddress.ToHttp();
                    return methodExecuteFunc(serviceAddress);
                }
                catch (Exception e)
                {
                    _logger.ErrorWithObject(e, errorLogObjects);
                    return Task.FromResult(new OperationResult<T>(ActionStatus.InternalServerError));
                }
            }
        }

        private Task<OperationResult<T>> SendAsync<T, TBody>(string url,
            string urlTemplate,
            string metricName,
            Dictionary<string, string> headers,
            string[] additionalLabels,
            TBody body,
            Func<string, Task<OperationResult<T>>> methodExecuteFunc,
            params object[] errorLogObjects)
        {
            using (_metricsTracingFactory.CreateLoggingMetricsTimer(metricName,
                $"{HttpMethod.Delete}:{urlTemplate ?? url}", additionalLabels))
            {
                try
                {
                    var serviceAddress = _serviceAddress.ToHttp();
                    return methodExecuteFunc(serviceAddress);
                }
                catch (Exception e)
                {
                    _logger.ErrorWithObject(e, new {body, additionalLabels});
                    return Task.FromResult(new OperationResult<T>(ActionStatus.InternalServerError));
                }
            }
        }
    }
}