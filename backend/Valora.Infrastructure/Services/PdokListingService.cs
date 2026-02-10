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
    private readonly IPdokListingMapper _mapper;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<PdokListingService> _logger;

    public PdokListingService(
        HttpClient httpClient,
        IMemoryCache cache,
        IContextReportService contextReportService,
        IPdokListingMapper mapper,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<PdokListingService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _contextReportService = contextReportService;
        _mapper = mapper;
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

            // 2. Map initial DTO
            var listing = _mapper.MapToDto(doc, pdokId, null, null, null);

            // 3. Enrich with Context Report
            // Check if address is valid and not the "Unknown Address" placeholder
            if (!string.IsNullOrWhiteSpace(listing.Address) &&
                !string.Equals(listing.Address, PdokListingMapper.UnknownAddress, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    // Using address as input for context report
                    var reportRequest = new ContextReportRequestDto(Input: listing.Address, RadiusMeters: 1000);
                    var reportDto = await _contextReportService.BuildAsync(reportRequest, cancellationToken);
                    
                    double? compositeScore = reportDto.CompositeScore;
                    double? safetyScore = null;
                    if (reportDto.CategoryScores.TryGetValue("Safety", out var sScore)) safetyScore = sScore;

                    // Map DTO to Domain Model
                    var contextReport = ListingMapper.MapToDomain(reportDto);

                    listing = listing with
                    {
                        ContextCompositeScore = compositeScore,
                        ContextSafetyScore = safetyScore,
                        ContextReport = contextReport
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to fetch context report for PDOK listing: {PdokId}", pdokId);
                }
            }

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
