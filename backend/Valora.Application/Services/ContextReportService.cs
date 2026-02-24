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
/// <remarks>
/// <para>
/// <strong>Sequence Overview:</strong>
/// </para>
/// <code>
/// mermaid
/// sequenceDiagram
///     participant API as Valora.Api
///     participant Service as ContextReportService
///     participant Resolver as PdokLocationResolver
///     participant Providers as External Data Sources
///
///     API->>Service: BuildAsync(input)
///     Service->>Resolver: Resolve Address
///     Resolver-->>Service: Lat/Lon & Admin Codes
///
///     par Fan-Out
///         Service->>Providers: Fetch CBS Stats
///         Service->>Providers: Fetch OSM Amenities
///         Service->>Providers: Fetch Air Quality
///     end
///
///     Providers-->>Service: Raw Data
///     Service->>Service: Normalize & Score (Fan-In)
///     Service-->>API: ContextReportDto
/// </code>
/// </remarks>
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
    /// This method acts as the orchestrator for the "Fan-Out" pattern. It does NOT query databases sequentially.
    /// Instead, it delegates the parallel fetching of data to <see cref="IContextDataProvider"/> (Fan-Out),
    /// waits for all tasks to complete, and then aggregates the results (Fan-In) into a single report.
    /// </para>
    /// <para>
    /// <strong>Why Real-Time?</strong><br/>
    /// We do not store pre-computed reports for every address in the Netherlands (too much data, stale too quickly).
    /// Instead, we generate them on-demand and cache them aggressively.
    /// </para>
    /// <para>
    /// Key Decisions:
    /// <list type="bullet">
    /// <item>
    /// <strong>Radius Clamping:</strong> The search radius is clamped between 200m and 5000m.
    /// Values > 5km cause excessive load on the Overpass API (OSM) and result in timeouts.
    /// </item>
    /// <item>
    /// <strong>High-Precision Caching:</strong> We resolve the location to coordinates first, then use
    /// 5-decimal precision (~1 meter) for the cache key. This ensures "Damrak 1" and "Damrak 1, Amsterdam"
    /// share the same expensive report generation.
    /// </item>
    /// </list>
    /// </para>
    /// </remarks>
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
        // We must resolve the address to coordinates first because all subsequent API calls (CBS, OSM)
        // require lat/lon or neighborhood codes, not raw address strings.
        var location = await _locationResolver.ResolveAsync(request.Input, cancellationToken);
        if (location is null)
        {
            throw new ValidationException(new[] { "Could not resolve input to a Dutch address." });
        }

        // 2. Check Report Cache using stable location key
        // We use a high-precision coordinate key (5 decimals) instead of the input string.
        // This ensures "Damrak 1" and "Damrak 1, Amsterdam" hit the same cache entry.
        var cacheKey = GetCacheKey(location, normalizedRadius);

        if (_cache.TryGetValue(cacheKey, out ContextReportDto? cached) && cached is not null)
        {
            return cached;
        }

        // 3. Fetch Data from Provider (Fan-Out)
        // This call triggers the parallel execution of all external API clients.
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
