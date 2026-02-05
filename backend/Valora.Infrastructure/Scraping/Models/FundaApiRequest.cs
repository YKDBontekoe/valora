namespace Valora.Infrastructure.Scraping.Models;

/// <summary>
/// Request body for the Funda Topposition API.
/// </summary>
internal record FundaApiRequest
{
    public List<string> AggregationType { get; init; } = [];
    public string CultureInfo { get; init; } = "nl";
    public string GeoInformation { get; init; } = "";
    public List<string> OfferingType { get; init; } = [];
    public int Page { get; init; } = 1;
    public FundaApiPriceFilter? Price { get; init; }
    public List<string> Zoning { get; init; } = [];
}

/// <summary>
/// Price filter for the Funda API request.
/// </summary>
internal record FundaApiPriceFilter
{
    public int LowerBound { get; init; }
    public string PriceRangeType { get; init; } = "SalePrice";
    public int? UpperBound { get; init; }
}
