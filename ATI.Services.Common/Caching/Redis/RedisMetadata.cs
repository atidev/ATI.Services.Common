using System.Collections.Concurrent;

namespace ATI.Services.Common.Caching.Redis
{
    public static class RedisMetadata
    {
        public const string InsertManyScriptKey = "InsertMany";
        
        public static readonly ConcurrentDictionary<string, byte[]> ScriptShaByScriptType = new();
    }
}