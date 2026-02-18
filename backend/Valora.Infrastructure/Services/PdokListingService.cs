using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Mappings;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Domain.Services;

namespace Valora.Infrastructure.Services;

/// <summary>
/// Provides listing data enriched with context reports by combining PDOK Locatieserver data
/// with Valora's context aggregation engine.
/// </summary>
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

    /// <summary>
    /// Retrieves detailed information for a PDOK location object and enriches it with a context report.
    /// </summary>
    /// <param name="pdokId">The unique ID from the PDOK Locatieserver.</param>
    /// <returns>A fully enriched ListingDto or null if not found.</returns>
    /// <remarks>
    /// <para>
    /// This method performs a multi-step enrichment process:
    /// 1. Look up the raw address details from PDOK.
    /// 2. Parse the response to extract the address string.
    /// 3. Call the <see cref="IContextReportService"/> to generate a context report for that address.
    /// 4. Extract specific metrics (Safety Score, WOZ Value) for the listing summary.
    /// 5. Map everything into a <see cref="ListingDto"/>.
    /// </para>
    /// </remarks>
    public async Task<ListingDto?> GetListingDetailsAsync(string pdokId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"pdok-listing:{pdokId}";
        if (_cache.TryGetValue(cacheKey, out ListingDto? cached))
        {
            return cached;
        }

        try
        {
            // 1. Lookup address details from PDOK Locatieserver
            // We use the 'lookup' API to get the full object details including geometry.
            var encodedId = Uri.EscapeDataString(pdokId);
            var lookupUrl = $"{_options.PdokBaseUrl.TrimEnd('/')}/bzk/locatieserver/search/v3_1/lookup?id={encodedId}&fl=*";
            var response = await _httpClient.GetFromJsonAsync<JsonElement>(lookupUrl, cancellationToken);
            
            if (!PdokListingMapper.TryParsePdokResponse(response, out var doc))
            {
                return null;
            }

            // 2. Extract Basic Info (Address string)
            var address = PdokListingMapper.GetString(doc, "weergavenaam");

            // 3. Enrich with Context Report
            // This triggers the internal "fan-out" to CBS/Overpass to get neighborhood stats.
            // We extract the composite and safety scores to display on the listing card.
            var (contextReport, compositeScore, safetyScore) = await FetchContextReportAsync(address, pdokId, cancellationToken);

            // 4. Fetch WOZ Value (Exclusively from CBS Context Data)
            // The context report includes housing metrics which may contain the average WOZ value for the neighborhood.
            var (wozValue, wozReferenceDate, wozValueSource) = contextReport?.EstimateWozValue(_timeProvider) ?? (null, null, null);

            // 5. Map to ListingDto
            var listing = PdokListingMapper.MapFromPdok(
                doc,
                pdokId,
                compositeScore,
                safetyScore,
                contextReport,
                wozValue,
                wozReferenceDate,
                wozValueSource);


            // Cache the enriched listing to avoid re-fetching heavy context reports.
            _cache.Set(cacheKey, listing, TimeSpan.FromMinutes(_options.PdokListingCacheMinutes));
            return listing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch PDOK listing details for {Id}", pdokId);
            throw;
        }
    }

    /// <summary>
    /// Helper method to fetch the context report and map it to the domain model.
    /// Handles failures gracefully by returning nulls instead of throwing, allowing the listing
    /// to be returned even if enrichment fails.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Design Decision: Graceful Degradation.</strong>
    /// If the context report service is down (e.g., CBS API failure) or times out,
    /// we still want to show the basic listing details (Address, ID) to the user.
    /// Therefore, we catch exceptions here and return a null report, rather than failing the whole request.
    /// </para>
    /// </remarks>
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
            // Log but don't crash. Listing without context is better than no listing.
            _logger.LogWarning(ex, "Failed to fetch context report for PDOK listing: {PdokId}", pdokId);
            return (null, null, null);
        }
    }
}
