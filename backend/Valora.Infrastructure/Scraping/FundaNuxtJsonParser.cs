using System.Text.Json;
using Microsoft.Extensions.Logging;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

internal static class FundaNuxtJsonParser
{
    /// <summary>
    /// Parses the raw Nuxt JSON state to find the listing data.
    /// <para>
    /// <strong>Why Breadth-First Search (BFS)?</strong>
    /// The Nuxt hydration JSON is a massive, deeply nested object that represents the entire application state.
    /// The location of the actual listing data changes frequently due to:
    /// 1. <strong>A/B Testing:</strong> Funda often changes the nesting structure (e.g., `payload.data` vs `payload.state.listing`).
    /// 2. <strong>Page Type:</strong> "Project" pages (new construction) have a different structure than "Resale" pages.
    /// </para>
    /// <para>
    /// <strong>The Solution: Signature Matching</strong>
    /// Instead of hardcoding a fragile path like `root.payload.data.listing`, we search for a "Fingerprint".
    /// We assume that any object containing *all three* of these keys is the listing object we want:
    /// - `features` (e.g. Energy Label, Year Built)
    /// - `media` (Photos, Videos)
    /// - `description` (The main text)
    /// This makes the parser incredibly robust to structural changes.
    /// </para>
    /// </summary>
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
