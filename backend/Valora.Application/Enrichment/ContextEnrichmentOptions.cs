namespace Valora.Application.Enrichment;

public class ContextEnrichmentOptions
{
    public string PdokBaseUrl { get; set; } = "https://api.pdok.nl";
    public string CbsBaseUrl { get; set; } = "https://opendata.cbs.nl/ODataApi/OData";
    public string OverpassBaseUrl { get; set; } = "https://overpass-api.de";
    public string LuchtmeetnetBaseUrl { get; set; } = "https://api.luchtmeetnet.nl";
    public int ResolverCacheMinutes { get; set; } = 1440;
    public int CbsCacheMinutes { get; set; } = 1440;
    public int AmenitiesCacheMinutes { get; set; } = 360;
    public int AirQualityCacheMinutes { get; set; } = 30;
    public int ReportCacheMinutes { get; set; } = 60;
    public int PdokListingCacheMinutes { get; set; } = 60;
}
