using System.Text.RegularExpressions;

namespace Valora.Infrastructure.Scraping;

public partial class FundaUrlParser : IFundaUrlParser
{
    public string? ExtractRegionFromUrl(string url)
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

    [GeneratedRegex(@"funda\.nl/(?:koop|huur)/([^/]+)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegionRegex();

    [GeneratedRegex(@"selected_area=.*?""([^""]+)""", RegexOptions.IgnoreCase)]
    private static partial Regex QueryRegionRegex();
}
