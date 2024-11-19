using System;
using System.Collections.Generic;

namespace ATI.Services.Common.Sql;

public class DataBaseOptions
{
    public string ConnectionString { get; set; }
    public TimeSpan Timeout { get; set; }
    public IDictionary<string, int> TimeoutDictionary { get; set; } = new Dictionary<string, int>();
    public TimeSpan? LongTimeRequest { get; set; }

    public int? KeepAlive { get; set; }
    public int? Port { get; set; }
    public string Server { get; set; }
    public string Database { get; set; }
    public string UserName { get; set; }
    public string Password { get; set; }
    public int? MinPoolSize { get; set; }
    public int? MaxPoolSize { get; set; }
    public int? ConnectTimeout { get; set; }
    public int? ConnectRetryCount { get; set; }
    public int? ConnectRetryInterval { get; set; }

    /// <summary>
    /// Доверять сертификату сервер
    /// </summary>
    /// <remarks>Ничего не делает для npgsql</remarks>
    public bool? TrustServerCertificate { get; set; }
}