namespace ATI.Services.Common.Caching.MemoryCaching
{
    public class LocalCacheEvent
    {
        public string InstanceKey { get; set; }
        public LocalCacheEventType EventType { get; set; }
        public string SetKey { get; set; }
        public string Key { get; set; }
    }
}