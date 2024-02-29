using System.Threading;
using Prometheus;

namespace ATI.Services.Common.Caching.Redis
{
    public class HitRatioCounter
    {
        private readonly Gauge _hitRatio;
        private readonly Gauge _hitRatioNetto;
        private int _hits;
        private int _misses;
        private int _missesInDb;

        public HitRatioCounter(string hitRatioCounterService, string[] lables)
        {
            _hitRatio = Prometheus.Metrics.CreateGauge($"{hitRatioCounterService}_hits_ratio", "", lables);
            _hitRatioNetto = Prometheus.Metrics.CreateGauge($"{hitRatioCounterService}_hits_ratio_netto", "", lables);
        }

        public void Hit(int count = 1)
        {
            Interlocked.Add(ref _hits, count);
        }

        public void Miss(int count = 1)
        {
            Interlocked.Add(ref _misses, count);
        }

        public void MissInDb(int count = 1)
        {
            Interlocked.Add(ref _missesInDb, count);
        }

        public void Update()
        {
            var hits = Interlocked.Exchange(ref _hits, 0);
            var misses = Interlocked.Exchange(ref _misses, 0);
            var missesInDb = Interlocked.Exchange(ref _misses, 0);
            var total = hits + misses;
            _hitRatio.Set(total > 0 ? 100.0 * ((double) hits / total) : 100.0);
            total -= missesInDb;
            _hitRatioNetto.Set(total > 0 ? 100.0 * ((double) hits / total) : 100.0);
        }
    }
}
