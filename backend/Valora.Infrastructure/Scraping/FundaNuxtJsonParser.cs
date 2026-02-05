using System.Text.Json;
using Microsoft.Extensions.Logging;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

internal static class FundaNuxtJsonParser
{
    public static FundaNuxtListingData? Parse(string json, ILogger? logger = null)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);

            var queue = new Queue<JsonElement>();
            queue.Enqueue(doc.RootElement);

            int safetyCounter = 0;
            while (queue.Count > 0 && safetyCounter++ < 10000)
            {
                var current = queue.Dequeue();

                if (current.ValueKind == JsonValueKind.Object)
                {
                    if (current.TryGetProperty("features", out _) &&
                        current.TryGetProperty("media", out _) &&
                        current.TryGetProperty("description", out _))
                    {
                        return current.Deserialize<FundaNuxtListingData>();
                    }

                    foreach (var prop in current.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                        {
                            queue.Enqueue(prop.Value);
                        }
                    }
                }
                else if (current.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in current.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object || item.ValueKind == JsonValueKind.Array)
                        {
                            queue.Enqueue(item);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to parse Nuxt state JSON");
        }

        return null;
    }
}
