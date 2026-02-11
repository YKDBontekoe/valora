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
            var address = PdokListingMapper.GetString(doc, "weergavenaam");

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

            // 6. Map to ListingDto
            var listing = PdokListingMapper.MapFromPdok(
                doc,
                pdokId,
                compositeScore,
                safetyScore,
                contextReport,
                wozValue,
                wozReferenceDate,
                wozValueSource);


            _cache.Set(cacheKey, listing, TimeSpan.FromMinutes(_options.PdokListingCacheMinutes));
            return listing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch PDOK listing details for {Id}", pdokId);
            throw;
        }
    }

}