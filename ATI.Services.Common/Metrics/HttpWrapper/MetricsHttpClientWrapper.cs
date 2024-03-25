using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Http;
using ATI.Services.Common.Logging;
using ATI.Services.Common.Variables;
using JetBrains.Annotations;
using NLog;

namespace ATI.Services.Common.Metrics.HttpWrapper;

/// <summary>
/// Для удобства лучше использовать ConsulMetricsHttpClientWrapper из ATI.Services.Consul
/// Он внутри себя инкапсулирует ConsulServiceAddress и MetricsFactory
/// </summary>
[PublicAPI]
public class MetricsHttpClientWrapper : IDisposable
{
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;
    private readonly Func<LogLevel, LogLevel> _logLevelOverride;
    private IHttpClientFactory _httpClientFactory;
    private string _httpClientFactoryName;

    private const string LogMessageTemplate =
        "Сервис:{0} в ответ на запрос [HTTP {1} {2}] вернул ответ с статус кодом {3}.";

    public MetricsHttpClientWrapper(MetricsHttpClientConfig config, IHttpClientFactory httpClientFactory = null)
    {
        Config = config;
        _logger = LogManager.GetLogger(Config.ServiceName);

        if (config.UseHttpClientFactory)
        {
            if (httpClientFactory == null)
            {
                const string message = "httpClientFactory is null";
                _logger.ErrorWithObject(null, message);
                throw new Exception(message);
            }
            
            _httpClientFactory = httpClientFactory;
            _httpClientFactoryName = config.ServiceName;
        }
        else
        {
            _httpClient = CreateHttpClient(config.Headers, config.PropagateActivity);
        }
            
        
        _logLevelOverride = Config.LogLevelOverride;
    }

    public MetricsHttpClientConfig Config { get; }

    private HttpClient CreateHttpClient(Dictionary<string, string> additionalHeaders, bool propagateActivity = true)
    {
        HttpClient httpClient;
        if (propagateActivity)
        {
            httpClient = new HttpClient();
        }
        else
        {
            httpClient = new HttpClient(new SocketsHttpHandler
            {
                ActivityHeadersPropagator = DistributedContextPropagator.CreateNoOutputPropagator()
            });
        }

        httpClient.Timeout = Config.Timeout;
        httpClient.SetBaseFields(ServiceVariables.ServiceAsClientName, ServiceVariables.ServiceAsClientHeaderName, additionalHeaders);

        return httpClient;
    }


    public async Task<OperationResult<TModel>> GetAsync<TModel>(Uri fullUrl, string metricName,
                                                                Dictionary<string, string> headers = null)
        => await SendAsync<TModel>(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Get, fullUrl, headers));


    public async Task<OperationResult<TModel>> GetAsync<TModel>(string serviceAddress, string metricName,
                                                                string url, Dictionary<string, string> headers = null)
        => await SendAsync<TModel>(metricName,
                                   new MetricsHttpClientWrapperHttpMessage(HttpMethod.Get, FullUri(serviceAddress, url), headers));


    public async Task<OperationResult<string>> GetAsync(string serviceAddress, string metricName, string url,
                                                        Dictionary<string, string> headers = null)
        => await SendAsync(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Get, FullUri(serviceAddress, url), headers));


    public async Task<OperationResult<string>> GetAsync(Uri fullUrl, string metricName,
                                                        Dictionary<string, string> headers = null)
        => await SendAsync(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Get, fullUrl, headers));


    public async Task<OperationResult<TResult>> PostAsync<TModel, TResult>(string serviceAddress, string metricName,
                                                                           string url, TModel model, Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName,
                                    new MetricsHttpClientWrapperHttpMessage(HttpMethod.Post, FullUri(serviceAddress, url), headers)
                                    {
                                        Content = Config.Serializer.Serialize(model)
                                    });


    public async Task<OperationResult<TResult>> PostAsync<TModel, TResult>(Uri fullUrl, string metricName,
                                                                           TModel model, Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Post, fullUrl, headers)
        {
            Content = Config.Serializer.Serialize(model)
        });


    public async Task<OperationResult<string>> PostAsync<TModel>(string serviceAddress, string metricName,
                                                                 string url, TModel model, Dictionary<string, string> headers = null)
        => await SendAsync(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Post, FullUri(serviceAddress, url), headers)
        {
            Content = Config.Serializer.Serialize(model)
        });


    public async Task<OperationResult<string>> PostAsync<TModel>(Uri fullUrl, string metricName, TModel model,
                                                                 Dictionary<string, string> headers = null)
        => await SendAsync(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Post, fullUrl, headers)
        {
            Content = Config.Serializer.Serialize(model)
        });


    public async Task<OperationResult<TResult>> PostAsync<TResult>(string serviceAddress, string metricName,
                                                                   string url, string rawContent, Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName,
                                    new MetricsHttpClientWrapperHttpMessage(HttpMethod.Post, FullUri(serviceAddress, url), headers)
                                    {
                                        Content = rawContent
                                    });


    public async Task<OperationResult<TResult>> PostAsync<TResult>(Uri fullUrl, string metricName,
                                                                   string rawContent, Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Post, fullUrl, headers)
        {
            Content = rawContent
        });


    public async Task<OperationResult<TResult>> PostAsync<TResult>(string serviceAddress, string metricName,
                                                                   string url, Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName,
                                    new MetricsHttpClientWrapperHttpMessage(HttpMethod.Post, FullUri(serviceAddress, url), headers));


    public async Task<OperationResult<TResult>> PostAsync<TResult>(Uri fullUrl, string metricName,
                                                                   Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Post, fullUrl, headers));


    public async Task<OperationResult<string>> PostAsync(string serviceAddress, string metricName, string url,
                                                         Dictionary<string, string> headers = null)
        => await SendAsync(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Post, FullUri(serviceAddress, url), headers));


    public async Task<OperationResult<string>> PostAsync(Uri fullUri, string metricName,
                                                         Dictionary<string, string> headers = null)
        => await SendAsync(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Post, fullUri, headers));


    public async Task<OperationResult<string>> PostAsync(string serviceAddress, string metricName, string url,
                                                         string rawContent, Dictionary<string, string> headers = null)
        => await SendAsync(metricName,
                           new MetricsHttpClientWrapperHttpMessage(HttpMethod.Post, FullUri(serviceAddress, url), headers) { Content = rawContent });


    public async Task<OperationResult<string>> PostAsync(Uri fullUri, string metricName, string rawContent,
                                                         Dictionary<string, string> headers = null)
        => await SendAsync(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Post, fullUri, headers) { Content = rawContent });


    public async Task<OperationResult<TResult>> PutAsync<TModel, TResult>(string serviceAddress, string metricName,
                                                                          string url, TModel model, Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName,
                                    new MetricsHttpClientWrapperHttpMessage(HttpMethod.Put, FullUri(serviceAddress, url), headers)
                                    {
                                        Content = Config.Serializer.Serialize(model)
                                    });


    public async Task<OperationResult<TResult>> PutAsync<TModel, TResult>(Uri fullUri, string metricName,
                                                                          TModel model, Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Put, fullUri, headers)
        {
            Content = Config.Serializer.Serialize(model)
        });


    public async Task<OperationResult<TResult>> PutAsync<TResult>(string serviceAddress, string metricName,
                                                                  string url, Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName,
                                    new MetricsHttpClientWrapperHttpMessage(HttpMethod.Put, FullUri(serviceAddress, url), headers));


    public async Task<OperationResult<TResult>> PutAsync<TResult>(Uri fullUri, string metricName,
                                                                  Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Put, fullUri, headers));


    public async Task<OperationResult<string>> PutAsync(string serviceAddress, string metricName, string url,
                                                        Dictionary<string, string> headers = null)
        => await SendAsync(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Put, FullUri(serviceAddress, url), headers));


    public async Task<OperationResult<string>> PutAsync(Uri fullUri, string metricName,
                                                        Dictionary<string, string> headers = null)
        => await SendAsync(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Put, fullUri, headers));


    public async Task<OperationResult<TResult>> DeleteAsync<TModel, TResult>(string serviceAddress,
                                                                             string metricName, string url, TModel model, Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName,
                                    new MetricsHttpClientWrapperHttpMessage(HttpMethod.Delete, FullUri(serviceAddress, url), headers)
                                    {
                                        Content = Config.Serializer.Serialize(model)
                                    });


    public async Task<OperationResult<TResult>> DeleteAsync<TResult>(string serviceAddress, string metricName,
                                                                     string url, Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName,
                                    new MetricsHttpClientWrapperHttpMessage(HttpMethod.Delete, FullUri(serviceAddress, url), headers));

    public async Task<OperationResult<TResult>> DeleteAsync<TResult>(Uri fullUri, string metricName,
                                                                     Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Delete, fullUri, headers));


    public async Task<OperationResult<string>> DeleteAsync(string serviceAddress, string metricName, string url,
                                                           Dictionary<string, string> headers = null)
        => await SendAsync(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Delete, FullUri(serviceAddress, url), headers));


    public async Task<OperationResult<string>> DeleteAsync(Uri fullUri, string metricName,
                                                           Dictionary<string, string> headers = null)
        => await SendAsync(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Delete, fullUri, headers));


    public async Task<OperationResult<TResult>> PatchAsync<TModel, TResult>(string serviceAddress,
                                                                            string metricName,
                                                                            string url, TModel model, Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName,
                                    new MetricsHttpClientWrapperHttpMessage(HttpMethod.Patch, FullUri(serviceAddress, url), headers)
                                    {
                                        Content = Config.Serializer.Serialize(model)
                                    });


    public async Task<OperationResult<TResult>> PatchAsync<TModel, TResult>(Uri fullUri, string metricName,
                                                                            TModel model, Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Patch, fullUri, headers)
        {
            Content = Config.Serializer.Serialize(model)
        });


    public async Task<OperationResult<TResult>> PatchAsync<TResult>(string serviceAddress, string metricName,
                                                                    string url, Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName,
                                    new MetricsHttpClientWrapperHttpMessage(HttpMethod.Patch, FullUri(serviceAddress, url), headers));


    public async Task<OperationResult<TResult>> PatchAsync<TResult>(Uri fullUri, string metricName,
                                                                    Dictionary<string, string> headers = null)
        => await SendAsync<TResult>(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Patch, fullUri, headers));


    public async Task<OperationResult<string>> PatchAsync<TModel>(string serviceAddress, string metricName,
                                                                  string url, TModel model, Dictionary<string, string> headers = null)
        => await SendAsync<string>(metricName,
                                   new MetricsHttpClientWrapperHttpMessage(HttpMethod.Patch, FullUri(serviceAddress, url), headers)
                                   {
                                       Content = Config.Serializer.Serialize(model)
                                   });


    public async Task<OperationResult<string>> PatchAsync<TModel>(Uri fullUri, string metricName,
                                                                  TModel model, Dictionary<string, string> headers = null)
        => await SendAsync<string>(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Patch, fullUri, headers)
        {
            Content = Config.Serializer.Serialize(model)
        });


    public async Task<OperationResult<string>> PatchAsync(string serviceAddress, string metricName, string url,
                                                          Dictionary<string, string> headers = null)
        => await SendAsync(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Patch, FullUri(serviceAddress, url), headers));


    public async Task<OperationResult<string>> PatchAsync(Uri fullUri, string metricName,
                                                          Dictionary<string, string> headers = null)
        => await SendAsync(metricName, new MetricsHttpClientWrapperHttpMessage(HttpMethod.Patch, fullUri, headers));


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

            var message = new MetricsHttpClientWrapperHttpMessage(method ?? HttpMethod.Put, fullUri, headers)
            {
                Content = model != null ? Config.Serializer.Serialize(model) : ""
            };

            using var requestMessage = message.ToRequestMessage(Config);

            using var responseMessage = await GetHttpClient().SendAsync(requestMessage);
            var responseContent = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);

            if (!responseMessage.IsSuccessStatusCode)
            {
                var logMessage = string.Format(LogMessageTemplate, Config.ServiceName, message.Method,
                                               message.FullUri, responseMessage.StatusCode);

                var logLevel = responseMessage.StatusCode == HttpStatusCode.InternalServerError
                                   ? _logLevelOverride(LogLevel.Error)
                                   : _logLevelOverride(LogLevel.Warn);
                _logger.LogWithObject(logLevel, ex: null, logMessage, logObjects: responseContent);
            }

            var result = new HttpResponseMessage<TResult>
            {
                StatusCode = responseMessage.StatusCode,
                Headers = responseMessage.Headers,
                TrailingHeaders = responseMessage.TrailingHeaders,
                ReasonPhrase = responseMessage.ReasonPhrase,
                Version = responseMessage.Version,
                RawContent = responseContent
            };

            try
            {
                result.Content = !string.IsNullOrEmpty(result.RawContent)
                                     ? Config.Serializer.Deserialize<TResult>(result.RawContent)
                                     : default;
            }
            catch (TaskCanceledException e) when (e.InnerException is TimeoutException)
            {
                _logger.LogWithObject(_logLevelOverride(LogLevel.Warn),
                                      e,
                                      logObjects: new
                                      {
                                          MetricName = metricName, FullUri = fullUri, Model = model,
                                          Headers = headers
                                      });
                return new OperationResult<HttpResponseMessage<TResult>>(ActionStatus.Timeout);
            }
            catch (Exception e)
            {
                _logger.LogWithObject(_logLevelOverride(LogLevel.Error),
                                      e,
                                      logObjects:
                                      new
                                      {
                                          MetricName = metricName, FullUri = fullUri, Model = model,
                                          Headers = headers,
                                          ResponseBody = result.RawContent
                                      });
            }

            return new(result, OperationResult.GetActionStatusByHttpStatusCode(result.StatusCode));
        }
        catch (Exception e)
        {
            _logger.LogWithObject(_logLevelOverride(LogLevel.Error),
                                  e,
                                  logObjects: new { MetricName = metricName, FullUri = fullUri, Model = model, Headers = headers });
            return new OperationResult<HttpResponseMessage<TResult>>(e);
        }
    }

    private async Task<OperationResult<TResult>> SendAsync<TResult>(string methodName, MetricsHttpClientWrapperHttpMessage clientWrapperHttpMessage)
    {
        try
        {
            if (clientWrapperHttpMessage.FullUri == null)
                return new OperationResult<TResult>(ActionStatus.InternalServerError,
                                                    "Адрес сообщения не указан (message.FullUri==null)");

            using var requestMessage = clientWrapperHttpMessage.ToRequestMessage(Config);

            using var responseMessage = await GetHttpClient().SendAsync(requestMessage);

            if (responseMessage.IsSuccessStatusCode)
            {
                var stream = await responseMessage.Content.ReadAsStreamAsync();
                var result = await Config.Serializer.DeserializeAsync<TResult>(stream);
                return new OperationResult<TResult>(result, OperationResult.GetActionStatusByHttpStatusCode(responseMessage.StatusCode));
            }

            var logMessage = string.Format(LogMessageTemplate, Config.ServiceName, clientWrapperHttpMessage.Method,
                                           clientWrapperHttpMessage.FullUri, responseMessage.StatusCode);
            var responseContent = await responseMessage.Content.ReadAsStringAsync();

            var logLevel = responseMessage.StatusCode == HttpStatusCode.InternalServerError
                               ? _logLevelOverride(LogLevel.Error)
                               : _logLevelOverride(LogLevel.Warn);
            _logger.LogWithObject(logLevel, ex: null, logMessage, logObjects: responseContent);

            return new OperationResult<TResult>(OperationResult.GetActionStatusByHttpStatusCode(responseMessage.StatusCode));
        }
        catch (TaskCanceledException e) when (e.InnerException is TimeoutException)
        {
            _logger.LogWithObject(_logLevelOverride(LogLevel.Error),
                                  e,
                                  logObjects: new { Method = methodName, Message = clientWrapperHttpMessage });
            return new OperationResult<TResult>(ActionStatus.Timeout);
        }
        catch (Exception e)
        {
            _logger.LogWithObject(_logLevelOverride(LogLevel.Error),
                                  e,
                                  logObjects: new { Method = methodName, Message = clientWrapperHttpMessage });
            return new OperationResult<TResult>(e);
        }
    }

    private async Task<OperationResult<string>> SendAsync(string methodName, MetricsHttpClientWrapperHttpMessage clientWrapperHttpMessage)
    {
        try
        {
            if (clientWrapperHttpMessage.FullUri == null)
                return new OperationResult<string>(ActionStatus.InternalServerError);

            using var requestMessage = clientWrapperHttpMessage.ToRequestMessage(Config);

            using var responseMessage = await GetHttpClient().SendAsync(requestMessage);
            var responseContent = await responseMessage.Content.ReadAsStringAsync();

            if (responseMessage.IsSuccessStatusCode)
                return new OperationResult<string>(responseContent, OperationResult.GetActionStatusByHttpStatusCode(responseMessage.StatusCode));

            var logMessage = string.Format(LogMessageTemplate, Config.ServiceName, clientWrapperHttpMessage.Method,
                                           clientWrapperHttpMessage.FullUri, responseMessage.StatusCode);

            var logLevel = responseMessage.StatusCode == HttpStatusCode.InternalServerError
                               ? _logLevelOverride(LogLevel.Error)
                               : _logLevelOverride(LogLevel.Warn);
            _logger.LogWithObject(logLevel, ex: null, logMessage, logObjects: responseContent);

            return new OperationResult<string>(responseContent,
                                               OperationResult.GetActionStatusByHttpStatusCode(responseMessage.StatusCode));
        }
        catch (TaskCanceledException e) when (e.InnerException is TimeoutException)
        {
            _logger.LogWithObject(_logLevelOverride(LogLevel.Error),
                                  e,
                                  logObjects: new { Method = methodName, Message = clientWrapperHttpMessage });
            return new OperationResult<string>(ActionStatus.Timeout);
        }
        catch (Exception e)
        {
            _logger.LogWithObject(_logLevelOverride(LogLevel.Error),
                                  e,
                                  logObjects: new { Method = methodName, Message = clientWrapperHttpMessage });
            return new OperationResult<string>(e);
        }
    }

    private HttpClient GetHttpClient()
    {
        return _httpClient ?? _httpClientFactory.CreateClient(_httpClientFactoryName);
    }

    private Uri FullUri(string serviceAddress, string url) =>
        serviceAddress != null ? new Uri(new Uri(serviceAddress), url ?? "") : null;

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}