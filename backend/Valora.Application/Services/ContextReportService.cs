using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Valora.Application.Common.Constants;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Application.Enrichment.Builders;
using Valora.Domain.Models;
using Valora.Domain.Services;

namespace Valora.Application.Services;

/// <summary>
/// Orchestrates the generation of context reports by aggregating data from multiple external sources.
/// </summary>
public sealed class ContextReportService : IContextReportService
{
    private readonly ILocationResolver _locationResolver;
    private readonly IContextDataProvider _contextDataProvider;
    private readonly ICacheService _cache;
    private readonly ContextEnrichmentOptions _options;
    private readonly ILogger<ContextReportService> _logger;

    public ContextReportService(
        ILocationResolver locationResolver,
        IContextDataProvider contextDataProvider,
        ICacheService cache,
        IOptions<ContextEnrichmentOptions> options,
        ILogger<ContextReportService> logger)
    {
        _locationResolver = locationResolver;
        _contextDataProvider = contextDataProvider;
        _cache = cache;
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Coordinates the retrieval of data from multiple public sources and builds a unified context report.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Architecture Pattern: Fan-Out / Fan-In</strong><br/>
    /// This method implements the core "Fan-Out" strategy that defines Valora's architecture.
    /// Rather than querying a local database for pre-existing reports, it acts as a real-time aggregator.
    /// </para>
    /// <para>
    /// <strong>The Data Flow:</strong>
    /// <list type="number">
    /// <item><strong>Resolve:</strong> Converts the input string (address or URL) into precise coordinates.</item>
    /// <item><strong>Check Cache:</strong> Uses a 5-decimal precision key (~1.1m resolution) to check for recent reports.</item>
    /// <item><strong>Fan-Out:</strong> If not cached, it delegates to <see cref="IContextDataProvider.GetSourceDataAsync"/>.
    /// This provider fires multiple tasks in parallel (Task.WhenAll) to fetch data from CBS, PDOK, and OSM simultaneously.</item>
    /// <item><strong>Fan-In:</strong> The results are normalized and scored by <see cref="ContextReportBuilder"/>.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Key Design Decisions:</strong>
    /// <list type="bullet">
    /// <item>
    /// <strong>No Scraping:</strong> We rely entirely on public APIs. This ensures legal compliance and data freshness.
    /// </item>
    /// <item>
    /// <strong>Radius Clamping (200m - 5km):</strong> We strictly enforce these limits. A radius larger than 5km
    /// triggers massive queries on OpenStreetMap (Overpass API), causing timeouts and memory spikes.
    /// A radius smaller than 200m is statistically insignificant for neighborhood context.
    /// </item>
    /// <item>
    /// <strong>Partial Failure Tolerance:</strong> If a secondary source (e.g., Luchtmeetnet) fails, we do NOT fail the request.
    /// Instead, we return a valid report with a 'Warning' flag. This ensures the app remains usable even during partial outages.
    /// </item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <param name="request">The request containing the input location and desired radius.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A fully populated <see cref="ContextReportDto"/> containing scores, metrics, and source attributions.</returns>
    /// <exception cref="ValidationException">Thrown if input is missing or cannot be resolved to a Dutch address.</exception>
    public async Task<ContextReportDto> BuildAsync(ContextReportRequestDto request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Input))
        {
            throw new ValidationException(new[] { "Input is required." });
        }

        // Radius is clamped to prevent excessive load on Overpass/external APIs.
        // Queries > 5km can cause timeouts or massive memory usage in the Overpass client.
        // Minimum 200m ensures we have a meaningful neighborhood scope.
        var normalizedRadius = Math.Clamp(request.RadiusMeters, ReportConstants.MinRadiusMeters, ReportConstants.MaxRadiusMeters);

        // 1. Resolve Location First
        var location = await _locationResolver.ResolveAsync(request.Input, cancellationToken);
        if (location is null)
        {
            throw new ValidationException(new[] { "Could not resolve input to a Dutch address." });
        }

        // 2. Check Report Cache using stable location key
        var cacheKey = GetCacheKey(location, normalizedRadius);

        if (_cache.TryGetValue(cacheKey, out ContextReportDto? cached) && cached is not null)
        {
            return cached;
        }

        // 3. Fetch Data from Provider (Fan-Out)
        // This invokes `Task.WhenAll` under the hood to fetch data simultaneously
        // from CBS, PDOK, Luchtmeetnet, and OSM.
        // Why? Fan-Out is essential for aggregating reports in < 1 second.
        // It enforces "partial failure tolerance": if one external source is down or slow,
        // it fails gracefully and adds a `Warning` instead of returning a 500 error to the user,
        // allowing the system to serve a degraded but useful report.
        var sourceData = await _contextDataProvider.GetSourceDataAsync(location, normalizedRadius, cancellationToken);
        var warnings = new List<string>(sourceData.Warnings);

        if (normalizedRadius != request.RadiusMeters)
        {
            warnings.Add($"Radius clamped from {request.RadiusMeters}m to {normalizedRadius}m to respect system limits.");
        }

        // 4. Build normalized metrics and compute scores (Fan-In)
        var report = ContextReportBuilder.Build(location, sourceData, warnings);

        // Cache the result for the configured duration (default: 24h)
        _cache.Set(cacheKey, report, TimeSpan.FromMinutes(_options.ReportCacheMinutes));
        return report;
    }

    private static string GetCacheKey(ResolvedLocationDto location, int radius)
    {
        // Key format: context-report:v3:{lat_f5}_{lon_f5}:{radius}
        // Using 5 decimal places (F5) gives precision to ~1 meter.
        var latKey = location.Latitude.ToString("F5", CultureInfo.InvariantCulture);
        var lonKey = location.Longitude.ToString("F5", CultureInfo.InvariantCulture);
        return $"{ReportConstants.CacheKeyPrefix}:{latKey}_{lonKey}:{radius}";
    }
}
