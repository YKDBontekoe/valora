namespace Valora.Infrastructure.Enrichment;

public static class UrlNormalizationUtils
{
    /// <summary>
    /// Normalizes raw user input into a searchable address string.
    /// Handles extraction of address parts from Funda URLs and generic URL hints.
    /// </summary>
    /// <remarks>
    /// Logic:
    /// 1. Checks if input is a valid absolute URI.
    /// 2. If URL contains common query params (query/address/location), use that value.
    /// 3. Else extracts the slug from the path (usually the last segment).
    /// 4. Decodes and replaces separators with spaces to form a search query.
    /// </remarks>
    public static string NormalizeInput(string input)
    {
        if (Uri.TryCreate(input, UriKind.Absolute, out var uri))
        {
            if (TryExtractAddressFromQuery(uri, out var queryHint))
            {
                return queryHint;
            }

            var segment = uri.Segments
                .Select(s => s.Trim('/'))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .LastOrDefault();

            if (!string.IsNullOrWhiteSpace(segment))
            {
                var normalizedSegment = Uri.UnescapeDataString(segment)
                    .Replace('-', ' ')
                    .Replace('_', ' ')
                    .Trim();

                // Funda slugs usually contain address text and are high-signal inputs.
                if (uri.Host.Contains("funda.nl", StringComparison.OrdinalIgnoreCase))
                {
                    return normalizedSegment;
                }

                // For non-Funda URLs, prefer a textual slug over passing the raw URL to PDOK.
                if (normalizedSegment.Any(char.IsLetter))
                {
                    return normalizedSegment;
                }
            }
        }

        return input.Trim();
    }

    private static bool TryExtractAddressFromQuery(Uri uri, out string value)
    {
        value = string.Empty;
        var query = uri.Query;
        if (string.IsNullOrWhiteSpace(query))
        {
            return false;
        }

        var candidateKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "q", "query", "address", "location", "loc"
        };

        foreach (var pair in query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length != 2)
            {
                continue;
            }

            var key = Uri.UnescapeDataString(kv[0]);
            if (!candidateKeys.Contains(key))
            {
                continue;
            }

            var decoded = Uri.UnescapeDataString(kv[1]).Replace('+', ' ').Trim();
            if (decoded.Any(char.IsLetterOrDigit))
            {
                value = decoded;
                return true;
            }
        }

        return false;
    }
}
