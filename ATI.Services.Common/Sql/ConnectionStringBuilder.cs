using Npgsql;

namespace ATI.Services.Common.Sql;

public static class ConnectionStringBuilder
{
    public static string BuildPostgresConnectionString(DataBaseOptions options)
    {
        var builder = new NpgsqlConnectionStringBuilder();

        if (options.ConnectionString != null)
        {
            builder.ConnectionString = options.ConnectionString;
            return builder.ToString();
        }

        if (options.Port != null)
        {
            builder.Port = options.Port.Value;
        }

        if (options.Server != null)
        {
            builder.Host = options.Server;
        }
        if (options.Database != null)
        {
            builder.Database = options.Database;
        }
        if (options.UserName != null)
        {
            builder.Username = options.UserName;
        }
        if (options.Password != null)
        {
            builder.Password = options.Password;
        }
        if (options.MinPoolSize != null)
        {
            builder.MinPoolSize = options.MinPoolSize.Value;
        }
        if (options.MaxPoolSize != null)
        {
            builder.MaxPoolSize = options.MaxPoolSize.Value;
        }
        if (options.ConnectTimeout != null)
        {
            builder.ConnectionLifetime = options.ConnectTimeout.Value;
        }
        if (options.KeepAlive != null)
        {
            builder.KeepAlive = options.KeepAlive.Value;
        }

        if (options.IdleConnectTimeout != null)
        {
            builder.ConnectionIdleLifetime = options.IdleConnectTimeout.Value;
        }
        

        builder.TrustServerCertificate = options.TrustServerCertificate ?? true;
        return builder.ToString();
    }
}