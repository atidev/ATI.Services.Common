using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ATI.Services.Common.Caching.Redis
{
    public class CacheHitRatioManager
    {
        private Timer _updateTimer;
        private readonly TimeSpan _updatePeriod;
        private readonly ConcurrentBag<HitRatioCounter> _counters;

        public CacheHitRatioManager(TimeSpan updatePeriod)
        {
            _updatePeriod = updatePeriod;
            _counters = new ConcurrentBag<HitRatioCounter>();
        }

        public HitRatioCounter CreateCounter(string hitRatioCounterService, params string[] lables)
        {
            var counter = new HitRatioCounter(hitRatioCounterService, lables);
            _counters.Add(counter);
            return counter;
        }

        public void Start()
        {
            _updateTimer = new Timer(_ => Update(), null, _updatePeriod, _updatePeriod);
        }

        public void Stop()
        {
            _updateTimer?.Dispose();
        }

        private void Update()
        {
            foreach (var counter in _counters)
            {
                counter.Update();
            }
        }
    }
}
