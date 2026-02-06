using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

internal static partial class FundaNuxtJsonParser
{
    /// <summary>
    /// Parses the raw Nuxt JSON state to find the listing data.
    /// <para>
    /// <strong>Why BFS?</strong>
    /// The structure of the Nuxt hydration state is highly volatile and deeply nested.
    /// The actual listing data might be at different depths depending on the page type (Project vs Listing)
    /// or A/B tests. Instead of hardcoding a path (e.g., `payload.data.listing`), we use a
    /// Breadth-First Search (BFS) to traverse the JSON object graph until we find an object
    /// that contains the signature keys: `features`, `media`, and `description`.
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

    /// <summary>
    /// Extracts the JSON content from the Nuxt hydration script tag.
    /// <para>
    /// <strong>Strategy:</strong>
    /// Funda uses Nuxt.js, which hydrates the client-side state via a `<script type="application/json">` tag.
    /// Instead of using a greedy regex (which is prone to catastrophic backtracking on large HTML),
    /// we iterate over all matching script tags and inspect their content for known keywords
    /// like "cachedListingData" or "features" + "media".
    /// </para>
    /// </summary>
    public static string? ExtractNuxtJson(string html)
    {
        // Simple regex to find the script content.
        // We look for script type="application/json" and iterate over them to find the one with the data.
        // This is safer than a greedy regex which might capture multiple script tags.

        var matches = NuxtScriptRegex().Matches(html);
        foreach (System.Text.RegularExpressions.Match m in matches)
        {
             var content = m.Groups[1].Value;
             // Check for key identifiers of the Nuxt hydration state
             if (content.Contains("cachedListingData") || (content.Contains("features") && content.Contains("media")))
             {
                 return content;
             }
        }

        return null;
    }

    [GeneratedRegex(@"<script type=""application/json""[^>]*>(.*?)</script>", RegexOptions.Singleline)]
    private static partial Regex NuxtScriptRegex();
}
