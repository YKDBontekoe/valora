using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Scraping;

public partial class FundaHtmlParser
{
    /// <summary>
    /// Parses listing cards from a funda.nl search results page.
    /// </summary>
    public IEnumerable<FundaListingCard> ParseSearchResults(string html)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Find all listing links using the pattern a[href^="/detail/koop/"]
        var listingLinks = doc.DocumentNode
            .SelectNodes("//a[starts-with(@href, '/detail/koop/') or starts-with(@href, '/detail/huur/')]");

        if (listingLinks == null)
            yield break;

        var seenUrls = new HashSet<string>();

        foreach (var link in listingLinks)
        {
            var href = link.GetAttributeValue("href", "");
            if (string.IsNullOrEmpty(href) || seenUrls.Contains(href))
                continue;

            seenUrls.Add(href);

            var fullUrl = href.StartsWith("http") ? href : $"https://www.funda.nl{href}";
            var fundaId = ExtractFundaIdFromUrl(href);

            if (string.IsNullOrEmpty(fundaId))
                continue;

            // Try to extract basic info from the card
            var cardText = link.InnerText;
            var priceMatch = PriceRegex().Match(cardText);

            yield return new FundaListingCard
            {
                FundaId = fundaId,
                Url = fullUrl,
                Price = priceMatch.Success ? ParsePrice(priceMatch.Value) : null
            };
        }
    }

    /// <summary>
    /// Parses a funda.nl listing detail page.
    /// </summary>
    public Listing? ParseDetailPage(string html, string fundaId, string url)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        // Extract address from h1
        var addressNode = doc.DocumentNode.SelectSingleNode("//h1");
        var address = addressNode?.InnerText.Trim();

        if (string.IsNullOrEmpty(address))
            return null;

        // Parse address components
        var (street, postalCode, city) = ParseAddress(address);

        // Extract price - look for € symbol with price pattern
        var priceNode = doc.DocumentNode.SelectNodes("//*[contains(text(), '€')]")?
            .FirstOrDefault(n => PriceRegex().IsMatch(n.InnerText));
        var price = priceNode != null ? ParsePrice(priceNode.InnerText) : null;

        // Extract features using dt/dd pattern
        var features = ExtractFeatures(doc);

        // Parse specific features
        var livingArea = ParseArea(features.GetValueOrDefault("Wonen") 
            ?? features.GetValueOrDefault("Gebruiksoppervlakte wonen"));
        var plotArea = ParseArea(features.GetValueOrDefault("Perceel") 
            ?? features.GetValueOrDefault("Perceeloppervlakte"));
        var rooms = ParseRooms(features.GetValueOrDefault("Aantal kamers"));
        var bedrooms = ParseBedrooms(features.GetValueOrDefault("Aantal kamers"));
        var propertyType = features.GetValueOrDefault("Soort woonhuis") 
            ?? features.GetValueOrDefault("Soort appartement");
        var status = features.GetValueOrDefault("Status");

        // Extract first image
        var imageNode = doc.DocumentNode.SelectSingleNode("//img[contains(@src, 'cloud.funda.nl')]");
        var imageUrl = imageNode?.GetAttributeValue("src", "");

        return new Listing
        {
            FundaId = fundaId,
            Address = street ?? address,
            City = city,
            PostalCode = postalCode,
            Price = price,
            Bedrooms = bedrooms,
            Bathrooms = null!, // Funda doesn't consistently show this
            LivingAreaM2 = livingArea,
            PlotAreaM2 = plotArea,
            PropertyType = propertyType,
            Status = status,
            Url = url,
            ImageUrl = imageUrl
        };
    }

    private Dictionary<string, string> ExtractFeatures(HtmlDocument doc)
    {
        var features = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var dtNodes = doc.DocumentNode.SelectNodes("//dt");
        if (dtNodes == null)
            return features;

        foreach (var dt in dtNodes)
        {
            var key = HttpUtility.HtmlDecode(dt.InnerText.Trim());
            var dd = dt.SelectSingleNode("following-sibling::dd");
            if (dd != null)
            {
                var value = HttpUtility.HtmlDecode(dd.InnerText.Trim());
                if (!string.IsNullOrEmpty(key) && !features.ContainsKey(key))
                {
                    features[key] = value;
                }
            }
        }

        return features;
    }

    private static string ExtractFundaIdFromUrl(string url)
    {
        // URL format: /detail/koop/amsterdam/appartement-12345678/
        var match = FundaIdRegex().Match(url);
        return match.Success ? match.Groups[1].Value : "";
    }

    private static decimal? ParsePrice(string text)
    {
        // Remove non-numeric characters except for digits
        var cleanedText = PriceCleanupRegex().Replace(text, "");
        if (decimal.TryParse(cleanedText, out var price) && price > 0)
            return price;
        return null;
    }

    private static int? ParseArea(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;
        var match = AreaRegex().Match(text);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var area))
            return area;
        return null;
    }

    private static int? ParseRooms(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;
        // "4 kamers (3 slaapkamers)" -> 4
        var match = RoomsRegex().Match(text);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var rooms))
            return rooms;
        return null;
    }

    private static int? ParseBedrooms(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;
        // "4 kamers (3 slaapkamers)" -> 3
        var match = BedroomsRegex().Match(text);
        if (match.Success && int.TryParse(match.Groups[1].Value, out var bedrooms))
            return bedrooms;
        return null;
    }

    private static (string? street, string? postalCode, string? city) ParseAddress(string address)
    {
        // Try to parse "Street 123 1234 AB Amsterdam" format
        var postalMatch = PostalCodeRegex().Match(address);
        if (postalMatch.Success)
        {
            var postalCode = postalMatch.Value;
            var parts = address.Split(postalCode);
            var street = parts.Length > 0 ? parts[0].Trim() : null;
            var city = parts.Length > 1 ? parts[1].Trim() : null;
            return (street, postalCode, city);
        }

        return (address, null, null);
    }

    [GeneratedRegex(@"€\s*[\d.,]+")]
    private static partial Regex PriceRegex();

    [GeneratedRegex(@"[^\d]")]
    private static partial Regex PriceCleanupRegex();

    [GeneratedRegex(@"-(\d+)[-/]")]
    private static partial Regex FundaIdRegex();

    [GeneratedRegex(@"(\d+)\s*m²")]
    private static partial Regex AreaRegex();

    [GeneratedRegex(@"(\d+)\s*kamer")]
    private static partial Regex RoomsRegex();

    [GeneratedRegex(@"(\d+)\s*slaapkamer")]
    private static partial Regex BedroomsRegex();

    [GeneratedRegex(@"\d{4}\s*[A-Z]{2}")]
    private static partial Regex PostalCodeRegex();
}

public class FundaListingCard
{
    public required string FundaId { get; init; }
    public required string Url { get; init; }
    public decimal? Price { get; init; }
}
