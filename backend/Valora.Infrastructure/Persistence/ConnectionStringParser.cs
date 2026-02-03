using Npgsql;

namespace Valora.Infrastructure.Persistence;

public static class ConnectionStringParser
{
    public static string BuildConnectionString(string? connectionUrl)
    {
        if (string.IsNullOrEmpty(connectionUrl))
            return string.Empty;

        // If it doesn't start with postgres:// or postgresql://, assume it's already a connection string
        if (!connectionUrl.StartsWith("postgres://") && !connectionUrl.StartsWith("postgresql://"))
        {
            return connectionUrl;
        }

        var databaseUri = new Uri(connectionUrl);
        var userInfo = databaseUri.UserInfo.Split(':');
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = databaseUri.Host,
            Port = databaseUri.Port == -1 ? 5432 : databaseUri.Port,
            Username = userInfo[0],
            Password = userInfo.Length > 1 ? userInfo[1] : null,
            Database = databaseUri.AbsolutePath.TrimStart('/')
        };

        return builder.ToString();
    }
}
