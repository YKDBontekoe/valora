using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Utilities;
using Valora.Application.DTOs.Map;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Valora.Application.Services;

public class MapService : IMapService
{
    private readonly IMapRepository _repository;
    private readonly IAmenityClient _amenityClient;
    private readonly ICbsGeoClient _cbsGeoClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<MapService> _logger;

    private const double MaxAggregatedSpan = 2.0;

    // Cache durations
    private static readonly TimeSpan CityInsightsCacheDuration    = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan PriceOverlayCacheDuration    = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan OverlayTilesCacheDuration    = TimeSpan.FromMinutes(10);

    public MapService(
        IMapRepository repository,
        IAmenityClient amenityClient,
        ICbsGeoClient cbsGeoClient,
        IMemoryCache cache,
        ILogger<MapService> logger)
    {
        _repository = repository;
        _amenityClient = amenityClient;
        _cbsGeoClient = cbsGeoClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<MapCityInsightDto>> GetCityInsightsAsync(CancellationToken cancellationToken = default)
    {
        const string cacheKey = "CityInsights";
        if (_cache.TryGetValue(cacheKey, out List<MapCityInsightDto>? cachedCityInsights) && cachedCityInsights is not null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cachedCityInsights;
        }

        _logger.LogDebug("Cache miss for {CacheKey}, fetching from DB", cacheKey);
        var cityInsights = await _repository.GetCityInsightsAsync(cancellationToken);
        _cache.Set(cacheKey, cityInsights, CityInsightsCacheDuration);
        return cityInsights;
    }

    public async Task<List<MapAmenityDto>> GetMapAmenitiesAsync(
        double minLat,
        double minLon,
        double maxLat,
        double maxLon,
        List<string>? types = null,
        CancellationToken cancellationToken = default)
    {
        GeoUtils.ValidateBoundingBox(minLat, minLon, maxLat, maxLon);
        if (minLon > maxLon) return [];

        return await _amenityClient.GetAmenitiesInBboxAsync(minLat, minLon, maxLat, maxLon, types, cancellationToken);
    }

    public async Task<List<MapOverlayDto>> GetMapOverlaysAsync(
        double minLat,
        double minLon,
        double maxLat,
        double maxLon,
        MapOverlayMetric metric,
        CancellationToken cancellationToken = default)
    {
        GeoUtils.ValidateBoundingBox(minLat, minLon, maxLat, maxLon);
        if (minLon > maxLon) return [];

        if (metric == MapOverlayMetric.PricePerSquareMeter)
        {
            return await CalculateAveragePriceOverlayAsync(minLat, minLon, maxLat, maxLon, cancellationToken);
        }

        return await _cbsGeoClient.GetNeighborhoodOverlaysAsync(minLat, minLon, maxLat, maxLon, metric, cancellationToken);
    }

    public async Task<List<MapAmenityClusterDto>> GetMapAmenityClustersAsync(
        double minLat,
        double minLon,
        double maxLat,
        double maxLon,
        double zoom,
        List<string>? types = null,
        CancellationToken cancellationToken = default)
    {
        GeoUtils.ValidateBoundingBox(minLat, minLon, maxLat, maxLon, MaxAggregatedSpan);
        if (minLon > maxLon) return [];

        double cellSize = GetCellSize(zoom);

        // Fetch amenities directly from client to bypass strict validation if needed,
        // but assuming client can handle the slightly larger area or we might need to chunk.
        // For now, we trust the client or standard validation if MaxAggregatedSpan is reasonable.
        var amenities = await _amenityClient.GetAmenitiesInBboxAsync(minLat, minLon, maxLat, maxLon, types, cancellationToken);

        return Utilities.AmenityClusterer.ClusterAmenities(amenities, cellSize);
    }

    public async Task<IReadOnlyList<MapOverlayTileDto>> GetMapOverlayTilesAsync(
        double minLat,
        double minLon,
        double maxLat,
        double maxLon,
        double zoom,
        MapOverlayMetric metric,
        CancellationToken cancellationToken = default)
    {
        GeoUtils.ValidateBoundingBox(minLat, minLon, maxLat, maxLon, MaxAggregatedSpan);
        if (minLon > maxLon) return [];

        double cellSize = GetCellSize(zoom);

        // Snap coordinates to cell-size grid to maximise cache hit rate.
        // A 1cm pan no longer busts the cache — only crossing a cell boundary does.
        var snappedMinLat = SnapToGrid(minLat, cellSize);
        var snappedMinLon = SnapToGrid(minLon, cellSize);
        var snappedMaxLat = SnapToGridCeil(maxLat, cellSize);
        var snappedMaxLon = SnapToGridCeil(maxLon, cellSize);
        int zoomBucket = (int)Math.Floor(zoom);

        var cacheKey = FormattableString.Invariant(
            $"MapOverlayTiles_{snappedMinLat}_{snappedMinLon}_{snappedMaxLat}_{snappedMaxLon}_{zoomBucket}_{metric}");

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<MapOverlayTileDto>? cachedTiles) && cachedTiles is not null)
        {
            _logger.LogDebug("Cache hit for overlay tiles, key={CacheKey}", cacheKey);
            return cachedTiles;
        }

        // Fetch detailed overlays (expand by one cell to avoid edge clipping)
        var overlays = await GetMapOverlaysAsync(
            snappedMinLat - cellSize,
            snappedMinLon - cellSize,
            snappedMaxLat + cellSize,
            snappedMaxLon + cellSize,
            metric,
            cancellationToken);

        var tiles = Utilities.OverlayRasterizer.RasterizeOverlays(
            overlays,
            snappedMinLat,
            snappedMinLon,
            snappedMaxLat,
            snappedMaxLon,
            cellSize);

        var readOnlyTiles = tiles.ToArray();
        _cache.Set(cacheKey, readOnlyTiles, OverlayTilesCacheDuration);
        return readOnlyTiles;
    }

    /// <summary>Snaps a coordinate to the nearest grid boundary at the given cell size.</summary>
    private static double SnapToGrid(double value, double cellSize) =>
        Math.Floor(value / cellSize) * cellSize;

    /// <summary>Snaps a coordinate to the next grid boundary at the given cell size.</summary>
    private static double SnapToGridCeil(double value, double cellSize) =>
        Math.Ceiling(value / cellSize) * cellSize;

    private async Task<List<MapOverlayDto>> CalculateAveragePriceOverlayAsync(
        double minLat, double minLon, double maxLat, double maxLon, CancellationToken ct)
    {
        // Round bounds to 2 decimal places (~1km grid) for a sensible cache granularity
        var cacheKey = FormattableString.Invariant(
            $"PriceOverlay_{Math.Round(minLat, 2)}_{Math.Round(minLon, 2)}_{Math.Round(maxLat, 2)}_{Math.Round(maxLon, 2)}");

        if (_cache.TryGetValue(cacheKey, out List<MapOverlayDto>? cachedOverlays) && cachedOverlays is not null)
        {
            _logger.LogDebug("Cache hit for price overlay, key={CacheKey}", cacheKey);
            return cachedOverlays;
        }

        var overlaysTask = _cbsGeoClient.GetNeighborhoodOverlaysAsync(minLat, minLon, maxLat, maxLon, MapOverlayMetric.PopulationDensity, ct);
        var listingDataTask = _repository.GetListingsPriceDataAsync(minLat, minLon, maxLat, maxLon, ct);

        await Task.WhenAll(overlaysTask, listingDataTask);

        var overlays = await overlaysTask;
        var listingData = await listingDataTask;

        var overlayResults = Utilities.PriceOverlayCalculator.CalculateAveragePriceOverlay(overlays, listingData);

        _cache.Set(cacheKey, overlayResults, PriceOverlayCacheDuration);
        return overlayResults;
    }

    private static double GetCellSize(double zoom)
    {
        if (zoom >= 13) return 0.005;
        if (zoom >= 11) return 0.01;
        if (zoom >= 9) return 0.02;
        if (zoom >= 7) return 0.05;
        return 0.1;
    }

}
