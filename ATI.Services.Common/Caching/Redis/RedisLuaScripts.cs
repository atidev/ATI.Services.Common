namespace ATI.Services.Common.Caching.Redis;

public static class RedisLuaScripts
{
    /// <summary>
    /// ARGV[#ARGV] - последний аргумент, время жизни ключа в миллисекундах
    /// </summary>
    public const string InsertMany = @"
for i=1, #KEYS do
    redis.call(""SET"", KEYS[i], ARGV[i], ""PX"", ARGV[#ARGV])
        end
return redis.status_reply('OK')
";
}