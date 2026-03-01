namespace Valora.Infrastructure.Persistence;

public static class ConnectionStringParser
{
    public static string BuildConnectionString(string? connectionUrl)
    {
        if (string.IsNullOrEmpty(connectionUrl))
            return string.Empty;

        return connectionUrl;
    }
}
