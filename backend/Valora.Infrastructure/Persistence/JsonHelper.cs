using System.Text.Json;

namespace Valora.Infrastructure.Persistence;

public static class JsonHelper
{
    private static readonly JsonSerializerOptions Options = new();

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public static T Deserialize<T>(string json) where T : new()
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new T();
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options) ?? new T();
        }
        catch (JsonException)
        {
            // Fallback for invalid JSON or type mismatch (e.g. object instead of array)
            // This prevents the application from crashing when reading corrupt data
            return new T();
        }
    }

    public static T? DeserializeNullable<T>(string? json) where T : class
    {
        if (string.IsNullOrWhiteSpace(json) || json.Trim() == "null")
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
