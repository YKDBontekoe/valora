using System.Text.RegularExpressions;

namespace Valora.Infrastructure.Scraping;

internal static partial class FundaValueParser
{
    public static int? ParseInt(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        var match = NumberRegex().Match(text);
        if (match.Success && int.TryParse(match.Value, out var num))
        {
            return num;
        }
        return null;
    }

    public static decimal? ParsePrice(string? priceText)
    {
        if (string.IsNullOrEmpty(priceText)) return null;

        // Dutch format: € 500.000 k.k. -> 500000
        // € 1.250,50 -> 1250.50

        // 1. Remove everything except digits, dots, commas
        var cleaned = PriceCleanupRegex().Replace(priceText, "").Trim();

        // 2. Handle Dutch formatting:
        //    - Remove thousands separators (dots)
        //    - Replace decimal separator (comma) with dot
        cleaned = cleaned.Replace(".", "").Replace(",", ".");

        if (decimal.TryParse(cleaned, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var price) && price > 0)
        {
            return price;
        }
        return null;
    }

    public static int? ParseBedrooms(string? text)
    {
        if (string.IsNullOrEmpty(text)) return null;

        var bedroomMatch = BedroomRegex().Match(text);
        if (bedroomMatch.Success && int.TryParse(bedroomMatch.Groups[1].Value, out var bedrooms))
        {
            return bedrooms;
        }

        return ParseInt(text);
    }

    public static (string Brand, int? Year) ParseCVBoiler(string? text)
    {
         if (string.IsNullOrEmpty(text)) return (string.Empty, null);

         var cvMatch = CVBoilerRegex().Match(text);
         if (cvMatch.Success)
         {
             var brand = cvMatch.Groups[1].Value.Trim();
             int? year = null;
             if (int.TryParse(cvMatch.Groups[2].Value, out var parsedYear))
             {
                 year = parsedYear;
             }
             return (brand, year);
         }

         return (text, null);
    }

    [GeneratedRegex(@"(\d+)\s*slaapkamer", RegexOptions.IgnoreCase)]
    private static partial Regex BedroomRegex();

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"(.+?)\s*\((\d{4})\)")]
    private static partial Regex CVBoilerRegex();

    [GeneratedRegex(@"[^\d.,]")]
    private static partial Regex PriceCleanupRegex();
}
