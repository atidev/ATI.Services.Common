using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using ATI.Services.Common.Extensions;
using Consul;
using NLog;


namespace ATI.Services.Consul
{
    public class ConsulServiceAddress : IDisposable
    {
        private readonly string _environment;
        private readonly string _serviceName;
        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        private List<ServiceEntry> _cachedServices = new();
        private List<ServiceEntry> CachedServices { get => _useCaching ? _cachedServices : GetServiceFromConsul().Result; set => _cachedServices = value; }
        private readonly Timer _updateCacheTimer;
        private readonly bool _useCaching;

        public ConsulServiceAddress(string serviceName, string environment, TimeSpan? timeToReload = null, bool useCaching = true)
        {
            if (timeToReload == null)
                timeToReload = TimeSpan.FromSeconds(5);

            _useCaching = useCaching;
            _environment = environment;
            _serviceName = serviceName;

            ReloadCache().GetAwaiter().GetResult();

            if (_useCaching)
                _updateCacheTimer = new Timer(async _ => await ReloadCache(), null, timeToReload.Value,
                    timeToReload.Value);
        }

        public List<ServiceEntry> GetAll()
        {
            return CachedServices;
        }

        public string ToHttp()
        {
            var serviceInfo = CachedServices.RandomItem();
            var address = string.IsNullOrWhiteSpace(serviceInfo?.Service?.Address)
                ? serviceInfo?.Node.Address
                : serviceInfo.Service.Address;

            if (string.IsNullOrWhiteSpace(address) || serviceInfo.Service == null)
            {
                _logger.Warn($"Не удалось взять настройки из консула для {_serviceName}:{_environment}");
                return null;
            }

            return $"http://{address}:{serviceInfo.Service.Port}";
        }

        public (string, int) GetAddressAndPort()
        {
            var serviceInfo = CachedServices.RandomItem();
            var address = string.IsNullOrWhiteSpace(serviceInfo?.Service?.Address)
                ? serviceInfo?.Node.Address
                : serviceInfo.Service.Address;

            if (string.IsNullOrWhiteSpace(address) || serviceInfo.Service == null)
            {
                _logger.Warn($"Не удалось взять настройки из консула для {_serviceName}:{_environment}");
                return default;
            }

            return (address, serviceInfo.Service.Port);
        }

        private async Task ReloadCache()
        {
            CachedServices = await GetServiceFromConsul();
        }

        private async Task<List<ServiceEntry>> GetServiceFromConsul()
        {
            try
            {
                using var cc = new ConsulClient();
                var fromConsul = await cc.Health.Service(_serviceName, _environment, true);
                if (fromConsul.StatusCode == HttpStatusCode.OK && fromConsul.Response.Length > 0)
                {
                    return fromConsul.Response.ToList();
                }
            }
            catch (Exception e)
            {
                _logger.Error(e);
            }
            return new List<ServiceEntry>();
        }


        public void Dispose()
        {
            _updateCacheTimer.Dispose();
        }
    }
}
