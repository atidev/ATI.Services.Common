using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Metrics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Text;
using ATI.Services.Common.Context;
using NLog;
using ATI.Services.Common.Extensions;
using ATI.Services.Common.Localization;
using ATI.Services.Common.Variables;
using JetBrains.Annotations;

namespace ATI.Services.Common.Tracing
{
    /// <summary>
    /// Для удобства лучше использовать ConsulMetricsHttpClientWrapper из ATI.Services.Consul
    /// Он внутри себя инкапсулирует ConsulServiceAddress и MetricsTracingFactory
    /// </summary>
    [PublicAPI]
    public class TracingHttpClientWrapper
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;
        private readonly MetricsTracingFactory _metricsTracingFactory;

        public TracingHttpClientWrapper(TracedHttpClientConfig config)
        {
            Config = config;
            _logger = LogManager.GetLogger(Config.ServiceName);
            _httpClient = CreateHttpClient(config.Headers);
            _metricsTracingFactory = MetricsTracingFactory.CreateTracingFactory(config.ServiceName);
        }

        public TracedHttpClientConfig Config { get; }

        private HttpClient CreateHttpClient(Dictionary<string, string> additionalHeaders)
        {
            var httpClient = new HttpClient { Timeout = Config.Timeout };
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!ServiceVariables.ServiceAsClientName.IsNullOrEmpty() &&
                !ServiceVariables.ServiceAsClientHeaderName.IsNullOrEmpty())
            {
                httpClient.DefaultRequestHeaders.Add(
                    ServiceVariables.ServiceAsClientHeaderName,
                    ServiceVariables.ServiceAsClientName);
            }

            if (additionalHeaders != null && additionalHeaders.Count > 0)
            {
                foreach (var header in additionalHeaders)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            return httpClient;
        }


        public async Task<OperationResult<TModel>> GetAsync<TModel>(Uri fullUrl, string metricName,
            Dictionary<string, string> headers = null)
            => await SendAsync<TModel>(metricName, new HttpMessage(HttpMethod.Get, fullUrl, headers));


        public async Task<OperationResult<TModel>> GetAsync<TModel>(string serviceAddress, string metricName,
            string url, Dictionary<string, string> headers = null)
            => await SendAsync<TModel>(metricName,
                new HttpMessage(HttpMethod.Get, FullUri(serviceAddress, url), headers));


        public async Task<OperationResult<string>> GetAsync(string serviceAddress, string metricName, string url,
            Dictionary<string, string> headers = null)
            => await SendAsync(metricName, new HttpMessage(HttpMethod.Get, FullUri(serviceAddress, url), headers));


        public async Task<OperationResult<string>> GetAsync(Uri fullUrl, string metricName,
            Dictionary<string, string> headers = null)
            => await SendAsync(metricName, new HttpMessage(HttpMethod.Get, fullUrl, headers));


        public async Task<OperationResult<TResult>> PostAsync<TModel, TResult>(string serviceAddress, string metricName,
            string url, TModel model, Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName,
                new HttpMessage(HttpMethod.Post, FullUri(serviceAddress, url), headers)
                {
                    Content = Config.Serializer.Serialize(model)
                });


        public async Task<OperationResult<TResult>> PostAsync<TModel, TResult>(Uri fullUrl, string metricName,
            TModel model, Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName, new HttpMessage(HttpMethod.Post, fullUrl, headers)
            {
                Content = Config.Serializer.Serialize(model)
            });


        public async Task<OperationResult<string>> PostAsync<TModel>(string serviceAddress, string metricName,
            string url, TModel model, Dictionary<string, string> headers = null)
            => await SendAsync(metricName, new HttpMessage(HttpMethod.Post, FullUri(serviceAddress, url), headers)
            {
                Content = Config.Serializer.Serialize(model)
            });


        public async Task<OperationResult<string>> PostAsync<TModel>(Uri fullUrl, string metricName, TModel model,
            Dictionary<string, string> headers = null)
            => await SendAsync(metricName, new HttpMessage(HttpMethod.Post, fullUrl, headers)
            {
                Content = Config.Serializer.Serialize(model)
            });


        public async Task<OperationResult<TResult>> PostAsync<TResult>(string serviceAddress, string metricName,
            string url, string rawContent, Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName,
                new HttpMessage(HttpMethod.Post, FullUri(serviceAddress, url), headers)
                {
                    Content = rawContent
                });


        public async Task<OperationResult<TResult>> PostAsync<TResult>(Uri fullUrl, string metricName,
            string rawContent, Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName, new HttpMessage(HttpMethod.Post, fullUrl, headers)
            {
                Content = rawContent
            });


        public async Task<OperationResult<TResult>> PostAsync<TResult>(string serviceAddress, string metricName,
            string url, Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName,
                new HttpMessage(HttpMethod.Post, FullUri(serviceAddress, url), headers));


        public async Task<OperationResult<TResult>> PostAsync<TResult>(Uri fullUrl, string metricName,
            Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName, new HttpMessage(HttpMethod.Post, fullUrl, headers));


        public async Task<OperationResult<string>> PostAsync(string serviceAddress, string metricName, string url,
            Dictionary<string, string> headers = null)
            => await SendAsync(metricName, new HttpMessage(HttpMethod.Post, FullUri(serviceAddress, url), headers));


        public async Task<OperationResult<string>> PostAsync(Uri fullUri, string metricName,
            Dictionary<string, string> headers = null)
            => await SendAsync(metricName, new HttpMessage(HttpMethod.Post, fullUri, headers));


        public async Task<OperationResult<string>> PostAsync(string serviceAddress, string metricName, string url,
            string rawContent, Dictionary<string, string> headers = null)
            => await SendAsync(metricName,
                new HttpMessage(HttpMethod.Post, FullUri(serviceAddress, url), headers) { Content = rawContent });


        public async Task<OperationResult<string>> PostAsync(Uri fullUri, string metricName, string rawContent,
            Dictionary<string, string> headers = null)
            => await SendAsync(metricName, new HttpMessage(HttpMethod.Post, fullUri, headers) { Content = rawContent });


        public async Task<OperationResult<TResult>> PutAsync<TModel, TResult>(string serviceAddress, string metricName,
            string url, TModel model, Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName,
                new HttpMessage(HttpMethod.Put, FullUri(serviceAddress, url), headers)
                {
                    Content = Config.Serializer.Serialize(model)
                });


        public async Task<OperationResult<TResult>> PutAsync<TModel, TResult>(Uri fullUri, string metricName,
            TModel model, Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName, new HttpMessage(HttpMethod.Put, fullUri, headers)
            {
                Content = Config.Serializer.Serialize(model)
            });


        public async Task<OperationResult<TResult>> PutAsync<TResult>(string serviceAddress, string metricName,
            string url, Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName,
                new HttpMessage(HttpMethod.Put, FullUri(serviceAddress, url), headers));


        public async Task<OperationResult<TResult>> PutAsync<TResult>(Uri fullUri, string metricName,
            Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName, new HttpMessage(HttpMethod.Put, fullUri, headers));


        public async Task<OperationResult<string>> PutAsync(string serviceAddress, string metricName, string url,
            Dictionary<string, string> headers = null)
            => await SendAsync(metricName, new HttpMessage(HttpMethod.Put, FullUri(serviceAddress, url), headers));


        public async Task<OperationResult<string>> PutAsync(Uri fullUri, string metricName,
            Dictionary<string, string> headers = null)
            => await SendAsync(metricName, new HttpMessage(HttpMethod.Put, fullUri, headers));


        public async Task<OperationResult<TResult>> DeleteAsync<TModel, TResult>(string serviceAddress,
            string metricName, string url, TModel model, Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName,
                new HttpMessage(HttpMethod.Delete, FullUri(serviceAddress, url), headers)
                {
                    Content = Config.Serializer.Serialize(model)
                });


        public async Task<OperationResult<TResult>> DeleteAsync<TResult>(string serviceAddress, string metricName,
            string url, Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName,
                new HttpMessage(HttpMethod.Delete, FullUri(serviceAddress, url), headers));

        public async Task<OperationResult<TResult>> DeleteAsync<TResult>(Uri fullUri, string metricName,
            Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName, new HttpMessage(HttpMethod.Delete, fullUri, headers));


        public async Task<OperationResult<string>> DeleteAsync(string serviceAddress, string metricName, string url,
            Dictionary<string, string> headers = null)
            => await SendAsync(metricName, new HttpMessage(HttpMethod.Delete, FullUri(serviceAddress, url), headers));


        public async Task<OperationResult<string>> DeleteAsync(Uri fullUri, string metricName,
            Dictionary<string, string> headers = null)
            => await SendAsync(metricName, new HttpMessage(HttpMethod.Delete, fullUri, headers));


        public async Task<OperationResult<TResult>> PatchAsync<TModel, TResult>(string serviceAddress,
            string metricName,
            string url, TModel model, Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName,
                new HttpMessage(HttpMethod.Patch, FullUri(serviceAddress, url), headers)
                {
                    Content = Config.Serializer.Serialize(model)
                });


        public async Task<OperationResult<TResult>> PatchAsync<TModel, TResult>(Uri fullUri, string metricName,
            TModel model, Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName, new HttpMessage(HttpMethod.Patch, fullUri, headers)
            {
                Content = Config.Serializer.Serialize(model)
            });


        public async Task<OperationResult<TResult>> PatchAsync<TResult>(string serviceAddress, string metricName,
            string url, Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName,
                new HttpMessage(HttpMethod.Patch, FullUri(serviceAddress, url), headers));


        public async Task<OperationResult<TResult>> PatchAsync<TResult>(Uri fullUri, string metricName,
            Dictionary<string, string> headers = null)
            => await SendAsync<TResult>(metricName, new HttpMessage(HttpMethod.Patch, fullUri, headers));


        public async Task<OperationResult<string>> PatchAsync<TModel>(string serviceAddress, string metricName,
            string url, TModel model, Dictionary<string, string> headers = null)
            => await SendAsync<string>(metricName,
                new HttpMessage(HttpMethod.Patch, FullUri(serviceAddress, url), headers)
                {
                    Content = Config.Serializer.Serialize(model)
                });


        public async Task<OperationResult<string>> PatchAsync<TModel>(Uri fullUri, string metricName,
            TModel model, Dictionary<string, string> headers = null)
            => await SendAsync<string>(metricName, new HttpMessage(HttpMethod.Patch, fullUri, headers)
            {
                Content = Config.Serializer.Serialize(model)
            });


        public async Task<OperationResult<string>> PatchAsync(string serviceAddress, string metricName, string url,
            Dictionary<string, string> headers = null)
            => await SendAsync(metricName, new HttpMessage(HttpMethod.Patch, FullUri(serviceAddress, url), headers));


        public async Task<OperationResult<string>> PatchAsync(Uri fullUri, string metricName,
            Dictionary<string, string> headers = null)
            => await SendAsync(metricName, new HttpMessage(HttpMethod.Patch, fullUri, headers));


        public async Task<OperationResult<HttpResponseMessage<TResult>>> SendAsync<TModel, TResult>(Uri fullUri,
            string metricName, TModel model,
            Dictionary<string, string> headers = null,
            HttpMethod method = null)
        {
            try
            {
                if (fullUri == null)
                    return new OperationResult<HttpResponseMessage<TResult>>(ActionStatus.InternalServerError,
                        "Адрес сообщения не указан (fullUri==null)");

                var message = new HttpMessage(method ?? HttpMethod.Put, fullUri, headers)
                {
                    Content = model != null ? Config.Serializer.Serialize(model) : ""
                };

                using (_metricsTracingFactory.CreateTracingTimer(
                           TraceHelper.GetHttpTracingInfo(fullUri.ToString(), metricName, message.Content)))
                {
                    using var requestMessage = message.ToRequestMessage(Config);

                    using var responseMessage = await _httpClient.SendAsync(requestMessage);

                    if (!responseMessage.IsSuccessStatusCode)
                    {
                        _logger.Warn(
                            $"Сервис:{Config.ServiceName} в ответ на запрос [HTTP {message.Method} {message.FullUri}] вернул ответ с статус кодом {responseMessage.StatusCode}.");
                    }

                    var result = new HttpResponseMessage<TResult>
                    {
                        StatusCode = responseMessage.StatusCode,
                        Headers = responseMessage.Headers,
                        TrailingHeaders = responseMessage.TrailingHeaders,
                        ReasonPhrase = responseMessage.ReasonPhrase,
                        Version = responseMessage.Version,
                        RawContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false)
                    };

                    try
                    {
                        using var reader = new JsonTextReader(new StringReader(result.RawContent));
                        result.Content = Config.Serializer.Deserialize<TResult>(reader);
                    }
                    catch (Exception e)
                    {
                        _logger.ErrorWithObject(e,
                            new
                            {
                                MetricName = metricName, FullUri = fullUri, Model = model, Headers = headers,
                                ResponseBody = result.RawContent
                            });
                    }

                    return new OperationResult<HttpResponseMessage<TResult>>(result);
                }
            }
            catch (Exception e)
            {
                _logger.ErrorWithObject(e,
                    new { MetricName = metricName, FullUri = fullUri, Model = model, Headers = headers });
                return new OperationResult<HttpResponseMessage<TResult>>(ActionStatus.InternalServerError);
            }
        }

        private async Task<OperationResult<TResult>> SendAsync<TResult>(string methodName, HttpMessage message)
        {
            using (_metricsTracingFactory.CreateTracingTimer(
                       TraceHelper.GetHttpTracingInfo(message.FullUri?.ToString() ?? "null", methodName,
                           message.Content)))
            {
                try
                {
                    if (message.FullUri == null)
                        return new OperationResult<TResult>(ActionStatus.InternalServerError,
                            "Адрес сообщения не указан (message.FullUri==null)");

                    using var requestMessage = message.ToRequestMessage(Config);

                    using var responseMessage = await _httpClient.SendAsync(requestMessage);

                    if (responseMessage.IsSuccessStatusCode)
                    {
                        using var streamReader = new StreamReader(await responseMessage.Content.ReadAsStreamAsync());
                        using var jsonReader = new JsonTextReader(streamReader);
                        var result = Config.Serializer.Deserialize<TResult>(jsonReader);

                        return new OperationResult<TResult>(result);
                    }

                    _logger.Warn(
                        $"Сервис:{Config.ServiceName} в ответ на запрос [HTTP {message.Method} {message.FullUri}] вернул ответ с статус кодом {responseMessage.StatusCode}.");

                    return new OperationResult<TResult>(
                        OperationResult.GetActionStatusByHttpStatusCode(responseMessage.StatusCode));
                }
                catch (Exception e)
                {
                    _logger.ErrorWithObject(e, new { Method = methodName, Message = message });
                    return new OperationResult<TResult>(ActionStatus.InternalServerError);
                }
            }
        }

        private async Task<OperationResult<string>> SendAsync(string methodName, HttpMessage message)
        {
            using (_metricsTracingFactory.CreateTracingTimer(
                       TraceHelper.GetHttpTracingInfo(message.FullUri.ToString(), methodName, message.Content)))
            {
                try
                {
                    if (message.FullUri == null)
                        return new OperationResult<string>(ActionStatus.InternalServerError);

                    using var requestMessage = message.ToRequestMessage(Config);

                    using var responseMessage = await _httpClient.SendAsync(requestMessage);
                    var responseContent = await responseMessage.Content.ReadAsStringAsync();

                    return new OperationResult<string>(responseContent,
                        OperationResult.GetActionStatusByHttpStatusCode(responseMessage.StatusCode));
                }
                catch (Exception e)
                {
                    _logger.ErrorWithObject(e, new { Method = methodName, Message = message });
                    return new OperationResult<string>(ActionStatus.InternalServerError);
                }
            }
        }

        private Uri FullUri(string serviceAddress, string url) =>
            serviceAddress != null ? new Uri(new Uri(serviceAddress), url ?? "") : null;

        private class HttpMessage
        {
            private readonly string ContentTypeHeaderName = "Content-Type";

            public HttpMessage(HttpMethod method, Uri fullUri, Dictionary<string, string> headers)
            {
                Method = method;
                FullUri = fullUri;
                Headers = new Dictionary<string, string>();
                ContentType = "application/json";

                if (headers == null)
                    return;

                foreach (var header in headers)
                {
                    if (string.Equals(header.Key, ContentTypeHeaderName,
                            StringComparison.InvariantCultureIgnoreCase))
                    {
                        ContentType = header.Value;
                    }
                    else
                    {
                        Headers.Add(header.Key, header.Value);
                    }
                }
            }

            public HttpMethod Method { get; }
            public string Content { get; init; }
            public Uri FullUri { get; }
            public Dictionary<string, string> Headers { get; }
            private string ContentType { get; }

            internal HttpRequestMessage ToRequestMessage(TracedHttpClientConfig config)
            {
                var msg = new HttpRequestMessage(Method, FullUri);

                if (config.ProxyServiceVariablesHeaders)
                    Headers.AddRange(AppHttpContext.HeadersAndValuesToProxy(config.HeadersToProxy));

                foreach (var header in Headers)
                    msg.Headers.Add(header.Key, header.Value);

                string acceptLanguage;
                if (config.AddCultureToRequest
                    && (acceptLanguage = FlowContext<RequestMetaData>.Current.AcceptLanguage) != null)
                    msg.Headers.Add("Accept-Language", acceptLanguage);


                if (string.IsNullOrEmpty(Content) == false)
                {
                    msg.Content = new StringContent(Content, Encoding.UTF8, ContentType);
                }

                return msg;
            }
        }
    }
}