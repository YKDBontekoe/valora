using System.Text.RegularExpressions;

namespace Valora.Infrastructure.Scraping;

internal static partial class FundaUrlParser
{
    private static readonly string[] AllowedHosts = ["funda.nl", "www.funda.nl"];

    public static string? ExtractRegionFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        var decodedUrl = Uri.UnescapeDataString(url);

        // URL format: https://www.funda.nl/koop/amsterdam/ or https://www.funda.nl/zoeken/koop?selected_area=...
        var regionMatch = UrlRegionRegex().Match(decodedUrl);
        if (regionMatch.Success)
        {
            return regionMatch.Groups[1].Value;
        }

        // Try to extract from query string
        var queryMatch = QueryRegionRegex().Match(decodedUrl);
        if (queryMatch.Success)
        {
            return queryMatch.Groups[1].Value;
        }

        return null;
    }

    public static int? ExtractGlobalIdFromUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        // URL format: https://www.funda.nl/detail/koop/amsterdam/appartement-.../43224373/
        // The GlobalId is typically the last numeric segment in the URL path
        // We look for matches of /digits or -digits and take the last one to avoid other numbers in slug
        var matches = GlobalIdRegex().Matches(url);
        if (matches.Count > 0)
        {
            var match = matches[^1]; // Last match
            if (int.TryParse(match.Groups[1].Value, out var id))
            {
                return id;
            }
        }
        return null;
    }

    /// <summary>
    /// Ensures the URL is absolute and belongs to the Funda domain.
    /// Returns empty string if invalid or untrusted.
    /// </summary>
    public static string EnsureAbsoluteUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;

        Uri? uri;
        // Check if it's already a valid absolute URL
        if (Uri.TryCreate(url, UriKind.Absolute, out uri))
        {
            // Security: Enforce allowed hosts to prevent SSRF
            if (AllowedHosts.Contains(uri.Host.ToLowerInvariant()))
            {
                return url;
            }

            // Reject external domains
            return string.Empty;
        }

        // If it's relative, ensure it starts with /
        var path = url.StartsWith("/") ? url : "/" + url;

        return "https://www.funda.nl" + path;
    }

    [GeneratedRegex(@"funda\.nl/(?:koop|huur)/([^/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegionRegex();

    [GeneratedRegex(@"selected_area=.*?""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex QueryRegionRegex();

    [GeneratedRegex(@"[/-](\d{6,})")]
    private static partial Regex GlobalIdRegex();
}
