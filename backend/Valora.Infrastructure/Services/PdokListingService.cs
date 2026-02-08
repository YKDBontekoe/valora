
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Infrastructure.Enrichment; // For ContextEnrichmentOptions

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
            var lookupUrl = $"{_options.PdokBaseUrl.TrimEnd('/')}/bzk/locatieserver/search/v3_1/lookup?id={pdokId}&fl=*";
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
            var street = GetString(doc, "straatnaam");
            var number = GetString(doc, "huisnummer");
            // var letter = GetString(doc, "huisletter"); 
            // var addition = GetString(doc, "huisnummertoevoeging");
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
                     contextReport = new Valora.Domain.Models.ContextReportModel(
                        new Valora.Domain.Models.ResolvedLocationModel(
                            reportDto.Location.Query, reportDto.Location.DisplayAddress,
                            reportDto.Location.Latitude, reportDto.Location.Longitude,
                            reportDto.Location.RdX, reportDto.Location.RdY,
                            reportDto.Location.MunicipalityCode, reportDto.Location.MunicipalityName,
                            reportDto.Location.DistrictCode, reportDto.Location.DistrictName,
                            reportDto.Location.NeighborhoodCode, reportDto.Location.NeighborhoodName,
                            reportDto.Location.PostalCode),
                        reportDto.SocialMetrics.Select(m => new Valora.Domain.Models.ContextMetricModel(m.Key, m.Label, m.Value, m.Unit, m.Score, m.Source, m.Note)).ToList(),
                        reportDto.CrimeMetrics.Select(m => new Valora.Domain.Models.ContextMetricModel(m.Key, m.Label, m.Value, m.Unit, m.Score, m.Source, m.Note)).ToList(),
                        reportDto.DemographicsMetrics.Select(m => new Valora.Domain.Models.ContextMetricModel(m.Key, m.Label, m.Value, m.Unit, m.Score, m.Source, m.Note)).ToList(),
                        reportDto.AmenityMetrics.Select(m => new Valora.Domain.Models.ContextMetricModel(m.Key, m.Label, m.Value, m.Unit, m.Score, m.Source, m.Note)).ToList(),
                        reportDto.EnvironmentMetrics.Select(m => new Valora.Domain.Models.ContextMetricModel(m.Key, m.Label, m.Value, m.Unit, m.Score, m.Source, m.Note)).ToList(),
                        reportDto.CompositeScore,
                        reportDto.CategoryScores.ToDictionary(k => k.Key, k => k.Value),
                        reportDto.Sources.Select(s => new Valora.Domain.Models.SourceAttributionModel(s.Source, s.Url, s.License, s.RetrievedAtUtc)).ToList(),
                        reportDto.Warnings.ToList()
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch context report for PDOK listing: {Address}", address);
                }
            }

            // 5. Map to ListingDto
            var listing = new ListingDto(
                Id: Guid.NewGuid(), 
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
                Description: $"Built in {yearBuilt}. Usage: {usage}.",
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
                ContextReport: contextReport
            );

            _cache.Set(cacheKey, listing, TimeSpan.FromMinutes(60));
            return listing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch PDOK listing details for {Id}", pdokId);
            return null;
        }
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
}
