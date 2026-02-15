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
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<PdokListingService> _logger;

    public PdokListingService(
        HttpClient httpClient,
        IMemoryCache cache,
        IContextReportService contextReportService,
        IOptions<ContextEnrichmentOptions> options,
        TimeProvider timeProvider,
        ILogger<PdokListingService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _contextReportService = contextReportService;
        _options = options.Value;
        _timeProvider = timeProvider;
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
            
            if (!PdokListingMapper.TryParsePdokResponse(response, out var doc))
            {
                return null;
            }

            // 2. Extract Basic Info
            var address = PdokListingMapper.GetString(doc, "weergavenaam");

            // 4. Enrich with Context Report
            var (contextReport, compositeScore, safetyScore) = await FetchContextReportAsync(address, pdokId, cancellationToken);

            // 5. Fetch WOZ Value (Exclusively from CBS Context Data)
            var (wozValue, wozReferenceDate, wozValueSource) = contextReport?.EstimateWozValue(_timeProvider) ?? (null, null, null);

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

    private async Task<(Valora.Domain.Models.ContextReportModel? Report, double? CompositeScore, double? SafetyScore)> FetchContextReportAsync(
        string? address,
        string pdokId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            return (null, null, null);
        }

        try
        {
            // Using address as input for context report
            var reportRequest = new ContextReportRequestDto(Input: address, RadiusMeters: 1000);
            var reportDto = await _contextReportService.BuildAsync(reportRequest, cancellationToken);

            double? safetyScore = null;
            if (reportDto.CategoryScores.TryGetValue(ContextScoreCalculator.CategorySafety, out var sScore)) safetyScore = sScore;

            // Map DTO to Domain Model
            var contextReport = ListingMapper.MapToDomain(reportDto);

            return (contextReport, reportDto.CompositeScore, safetyScore);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch context report for PDOK listing: {PdokId}", pdokId);
            return (null, null, null);
        }
    }
}
