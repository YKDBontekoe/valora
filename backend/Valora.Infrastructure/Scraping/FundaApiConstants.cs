namespace Valora.Infrastructure.Scraping;

internal static class FundaApiConstants
{
    public const string ToppositionApiUrl = "https://www.funda.nl/api/topposition/v2/search";
    public const string SummaryApiBaseUrl = "https://www.funda.nl/api/detail-summary/v2/getsummary/";
    public const string ContactApiBaseUrl = "https://contacts-bff.funda.io/api/v3/listings/";
    public const string FiberApiBaseUrl = "https://kpnopticfiber.funda.io/api/v1/";

    public const string PriceTypeSale = "SalePrice";
    public const string OfferingTypeBuy = "buy";
    public const string OfferingTypeRent = "rent";
    public const string AggregationTypeProject = "project";
    public const string AggregationTypeListing = "listing";
    public const string ZoningResidential = "residential";
    public const string CultureNl = "nl";
}
