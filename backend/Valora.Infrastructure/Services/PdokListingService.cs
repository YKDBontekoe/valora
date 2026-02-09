using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Mappings;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;

namespace Valora.Infrastructure.Services;

public class PdokListingService : IPdokListingService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly IContextReportService _contextReportService;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<PdokListingService> _logger;

    public PdokListingService(
        HttpClient httpClient,
        IMemoryCache cache,
        IContextReportService contextReportService,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<PdokListingService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _contextReportService = contextReportService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ListingDto?> GetListingDetailsAsync(string pdokId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"pdok-listing:{pdokId}";
        if (_cache.TryGetValue(cacheKey, out ListingDto? cached))
        {
            return cached;
        }

        try
        {
            // 1. Lookup address details
            var encodedId = Uri.EscapeDataString(pdokId);
            var lookupUrl = $"{_options.PdokBaseUrl.TrimEnd('/')}/bzk/locatieserver/search/v3_1/lookup?id={encodedId}&fl=*";
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(lookupUrl, cancellationToken);
            
            if (!response.TryGetProperty("response", out var responseObj) || 
                !responseObj.TryGetProperty("docs", out var docs) || 
                docs.GetArrayLength() == 0)
            {
                return null;
            }

            var doc = docs[0];

            // 2. Extract Basic Info
            var address = GetString(doc, "weergavenaam");
            var city = GetString(doc, "woonplaatsnaam");
            var postcode = GetString(doc, "postcode");
            var lat = TryParseCoordinate(GetString(doc, "centroide_ll"), true);
            var lon = TryParseCoordinate(GetString(doc, "centroide_ll"), false);

            // 3. Extract Building Info (Year Built, Area, Usage)
            int? yearBuilt = TryParseInt(GetString(doc, "bouwjaar"));
            int? area = TryParseInt(GetString(doc, "oppervlakte"));
            var usage = GetString(doc, "gebruiksdoelverblijfsobject"); // e.g. "woonfunctie"

            // 4. Enrich with Context Report
            Valora.Domain.Models.ContextReportModel? contextReport = null;
            double? compositeScore = null;
            double? safetyScore = null;

            if (!string.IsNullOrWhiteSpace(address))
            {
                try
                {
                    // Using address as input for context report
                    var reportRequest = new ContextReportRequestDto(Input: address, RadiusMeters: 1000);
                    var reportDto = await _contextReportService.BuildAsync(reportRequest, cancellationToken);
                    
                    compositeScore = reportDto.CompositeScore;
                    if (reportDto.CategoryScores.TryGetValue("Safety", out var sScore)) safetyScore = sScore;

                    // Map DTO to Domain Model
                    contextReport = ListingMapper.MapToDomain(reportDto);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch context report for PDOK listing: {PdokId}", pdokId);
                }
            }

            // 5. Fetch WOZ Value (Exclusively from CBS Context Data)
            int? wozValue = null;
            DateTime? wozReferenceDate = null;
            string? wozValueSource = null;

            if (contextReport != null)
            {
                var avgWozMetric = contextReport.SocialMetrics.FirstOrDefault(m => m.Key == "average_woz");
                if (avgWozMetric?.Value.HasValue == true)
                {
                    // Value is in k€ (e.g. 450), convert to absolute value
                    wozValue = (int)(avgWozMetric.Value.Value * 1000);
                    wozValueSource = "CBS Neighborhood Average";
                    // CBS data is typically from the previous year
                    wozReferenceDate = new DateTime(DateTime.UtcNow.Year - 1, 1, 1);
                }
            }

            // 5. Map to ListingDto
            var listing = new ListingDto(
                Id: GenerateStableId(pdokId), 
                FundaId: pdokId, // Store PDOK ID here for reference
                Address: address ?? "Unknown Address",
                City: city,
                PostalCode: postcode,
                Price: null, // Not available
                Bedrooms: null, // Not available
                Bathrooms: null, // Not available
                LivingAreaM2: area,
                PlotAreaM2: null,
                PropertyType: usage, 
                Status: "Unknown",
                Url: null,
                ImageUrl: null, 
                ListedDate: DateTime.UtcNow,
                CreatedAt: DateTime.UtcNow,
                Description: BuildDescription(yearBuilt, usage),
                EnergyLabel: null,
                YearBuilt: yearBuilt,
                ImageUrls: new List<string>(),
                OwnershipType: null,
                CadastralDesignation: null,
                VVEContribution: null,
                HeatingType: null,
                InsulationType: null,
                GardenOrientation: null,
                HasGarage: false,
                ParkingType: null,
                AgentName: null,
                VolumeM3: null,
                BalconyM2: null,
                GardenM2: null,
                ExternalStorageM2: null,
                Features: new Dictionary<string, string>(),
                Latitude: lat,
                Longitude: lon,
                VideoUrl: null,
                VirtualTourUrl: null,
                FloorPlanUrls: new List<string>(),
                BrochureUrl: null,
                RoofType: null,
                NumberOfFloors: null,
                ConstructionPeriod: null,
                CVBoilerBrand: null,
                CVBoilerYear: null,
                BrokerPhone: null,
                BrokerLogoUrl: null,
                FiberAvailable: null,
                PublicationDate: null,
                IsSoldOrRented: false,
                Labels: new List<string>(),
                ContextCompositeScore: compositeScore,
                ContextSafetyScore: safetyScore,
                ContextReport: contextReport,
                
                // WOZ
                WozValue: wozValue,
                WozReferenceDate: wozReferenceDate,
                WozValueSource: wozValueSource
            );


            _cache.Set(cacheKey, listing, TimeSpan.FromMinutes(_options.PdokListingCacheMinutes));
            return listing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch PDOK listing details for {Id}", pdokId);
            throw;
        }
    }

    private static string? BuildDescription(int? yearBuilt, string? usage)
    {
        var parts = new List<string>();
        if (yearBuilt.HasValue)
        {
            parts.Add($"Built in {yearBuilt}");
        }
        if (!string.IsNullOrWhiteSpace(usage))
        {
            parts.Add($"Usage: {usage}");
        }

        if (parts.Count == 0) return null;
        return string.Join(". ", parts) + ".";
    }

    private string? GetString(JsonElement doc, string key)
    {
        if (doc.TryGetProperty(key, out var prop))
        {
            return prop.ToString();
        }
        return null;
    }

    private int? TryParseInt(string? value)
    {
        if (int.TryParse(value, out var result)) return result;
        return null;
    }

    private double? TryParseCoordinate(string? wkt, bool isLat)
    {
        // POINT(lon lat)
        if (string.IsNullOrEmpty(wkt) || !wkt.StartsWith("POINT(") || !wkt.EndsWith(")")) return null;
        
        var content = wkt.Substring(6, wkt.Length - 7);
        var parts = content.Split(' ');
        if (parts.Length != 2) return null;

        if (double.TryParse(parts[isLat ? 1 : 0], NumberStyles.Any, CultureInfo.InvariantCulture, out var coord))
        {
            return coord;
        }
        return null;
    }

    private static Guid GenerateStableId(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}