using System.Text.RegularExpressions;

namespace Valora.Infrastructure.Scraping;

internal static partial class FundaUrlParser
{
    public static string? ExtractRegionFromUrl(string url)
    {
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
        // URL format: https://www.funda.nl/detail/koop/amsterdam/appartement-.../43224373/
        // The GlobalId is typically the last numeric segment in the URL path
        var match = GlobalIdRegex().Match(url);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var id))
        {
            return id;
        }
        return null;
    }

    public static string EnsureAbsoluteUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return string.Empty;

        // Ensure we have a valid Absolute URL
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            // Try to construct if it's relative? Funda usually gives us full URLs.
            if (url.StartsWith("/"))
            {
                return "https://www.funda.nl" + url;
            }
        }

        return url;
    }

    [GeneratedRegex(@"funda\.nl/(?:koop|huur)/([^/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegionRegex();

    [GeneratedRegex(@"selected_area=.*?""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex QueryRegionRegex();

    [GeneratedRegex(@"/(\d{6,})", RegexOptions.IgnoreCase)]
    private static partial Regex GlobalIdRegex();
}
