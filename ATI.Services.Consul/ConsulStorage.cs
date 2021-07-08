using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ATI.Services.Common.Behaviors;
using ATI.Services.Common.Extensions;
using ATI.Services.Common.Logging;
using Consul;
using JetBrains.Annotations;
using Newtonsoft.Json;
using NLog;

namespace ATI.Services.Consul
{
    [PublicAPI]
    public class ConsulStorage : IDisposable
    {
        private readonly Logger _logger;
        private readonly ConsulClient _consul;
        private readonly JsonSerializer _jsonSerializer;
        
        public ConsulStorage()
        {
            _logger = LogManager.GetCurrentClassLogger();
            _consul = new ConsulClient();
            _jsonSerializer = new JsonSerializer();
        }
        public async Task<OperationResult<T>> TryGetAsync<T>(string key)
        {
            try
            {
                var consulResponse = await _consul.KV.Get(key);
                if (consulResponse.StatusCode != HttpStatusCode.OK)
                    return new OperationResult<T>(ActionStatus.InternalServerError);

                var json = Encoding.UTF8.GetString(consulResponse.Response.Value);
                var result = JsonConvert.DeserializeObject<T>(json);
                return new OperationResult<T>(result);
            }
            catch (Exception e)
            {
                _logger.ErrorWithObject(e,new { Key = key});
                return new OperationResult<T>(ActionStatus.InternalServerError);
            }
        }

        public async Task<OperationResult<T>> PutAsync<T>(string key, T value)
        {
            try
            {
                var putPair = new KVPair(key)
                {
                    Value = _jsonSerializer.ToJsonBytes(value)
                };

                var putAttempt = await _consul.KV.Put(putPair);

                if (putAttempt.StatusCode != HttpStatusCode.OK)
                {
                    return new OperationResult<T>(ActionStatus.InternalServerError);
                }

                return await TryGetAsync<T>(key);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
                return new OperationResult<T>(ActionStatus.InternalServerError);
            }
        }

        public void Dispose()
        {
            _consul?.Dispose();
        }
    }
}