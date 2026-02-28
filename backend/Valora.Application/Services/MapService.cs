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
        if (_cache.TryGetValue(cacheKey, out List<MapCityInsightDto>? cached) && cached is not null)
        {
            _logger.LogDebug("Cache hit for {CacheKey}", cacheKey);
            return cached;
        }

        _logger.LogDebug("Cache miss for {CacheKey}, fetching from DB", cacheKey);
        var result = await _repository.GetCityInsightsAsync(cancellationToken);
        _cache.Set(cacheKey, result, CityInsightsCacheDuration);
        return result;
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
        ValidateAggregatedBoundingBox(minLat, minLon, maxLat, maxLon);
        if (minLon > maxLon) return [];

        double cellSize = GetCellSize(zoom);

        // Fetch amenities directly from client to bypass strict validation if needed,
        // but assuming client can handle the slightly larger area or we might need to chunk.
        // For now, we trust the client or standard validation if MaxAggregatedSpan is reasonable.
        var amenities = await _amenityClient.GetAmenitiesInBboxAsync(minLat, minLon, maxLat, maxLon, types, cancellationToken);

        var clusters = amenities
            .GroupBy(a => (
                Lat: Math.Floor(a.Latitude / cellSize),
                Lon: Math.Floor(a.Longitude / cellSize)
            ))
            .Select(groupedAmenities =>
            {
                var count = groupedAmenities.Count();
                var lat = (groupedAmenities.Key.Lat * cellSize) + (cellSize / 2);
                var lon = (groupedAmenities.Key.Lon * cellSize) + (cellSize / 2);

                var typeCounts = groupedAmenities.GroupBy(a => a.Type)
                    .ToDictionary(typeGroup => typeGroup.Key, typeGroup => typeGroup.Count());

                return new MapAmenityClusterDto(lat, lon, count, typeCounts);
            })
            .ToList();

        return clusters;
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
        ValidateAggregatedBoundingBox(minLat, minLon, maxLat, maxLon);
        if (minLon > maxLon) return [];

        double cellSize = GetCellSize(zoom);

        // Snap coordinates to cell-size grid to maximise cache hit rate.
        // A 1cm pan no longer busts the cache — only crossing a cell boundary does.
        var snappedMinLat = SnapToGrid(minLat, cellSize);
        var snappedMinLon = SnapToGrid(minLon, cellSize);
        var snappedMaxLat = SnapToGrid(maxLat, cellSize);
        var snappedMaxLon = SnapToGrid(maxLon, cellSize);
        int zoomBucket = (int)Math.Floor(zoom);

        var cacheKey = FormattableString.Invariant(
            $"MapOverlayTiles_{snappedMinLat}_{snappedMinLon}_{snappedMaxLat}_{snappedMaxLon}_{zoomBucket}_{metric}");

        if (_cache.TryGetValue(cacheKey, out IReadOnlyList<MapOverlayTileDto>? cachedTiles) && cachedTiles is not null)
        {
            _logger.LogDebug("Cache hit for overlay tiles, key={CacheKey}", cacheKey);
            return cachedTiles;
        }

        // Fetch detailed overlays (expand by one cell to avoid edge clipping)
        var overlays = await _cbsGeoClient.GetNeighborhoodOverlaysAsync(
            snappedMinLat - cellSize,
            snappedMinLon - cellSize,
            snappedMaxLat + cellSize,
            snappedMaxLon + cellSize,
            metric,
            cancellationToken);

        var tiles = new List<MapOverlayTileDto>();

        // Pre-parse geometries for performance
        var parsedOverlays = overlays.Select(overlay =>
            (Dto: overlay, Geometry: GeoUtils.ParseGeometry(overlay.GeoJson))
        ).ToList();

        // Build spatial index
        double indexCellSize = cellSize * 5;
        var spatialIndex = new Dictionary<(int, int), List<(MapOverlayDto Dto, GeoUtils.ParsedGeometry Geometry)>>();

        foreach (var parsedOverlay in parsedOverlays)
        {
            int minX = (int)Math.Floor(parsedOverlay.Geometry.BBox.MinLon / indexCellSize);
            int maxX = (int)Math.Floor(parsedOverlay.Geometry.BBox.MaxLon / indexCellSize);
            int minY = (int)Math.Floor(parsedOverlay.Geometry.BBox.MinLat / indexCellSize);
            int maxY = (int)Math.Floor(parsedOverlay.Geometry.BBox.MaxLat / indexCellSize);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var key = (x, y);
                    if (!spatialIndex.TryGetValue(key, out var list))
                    {
                        list = new List<(MapOverlayDto Dto, GeoUtils.ParsedGeometry Geometry)>();
                        spatialIndex[key] = list;
                    }
                    list.Add(parsedOverlay);
                }
            }
        }

        // Rasterize: iterate by cellSize, center each tile
        for (double lat = snappedMinLat + cellSize / 2; lat < snappedMaxLat; lat += cellSize)
        {
            int gridY = (int)Math.Floor(lat / indexCellSize);

            for (double lon = snappedMinLon + cellSize / 2; lon < snappedMaxLon; lon += cellSize)
            {
                int gridX = (int)Math.Floor(lon / indexCellSize);
                var key = (gridX, gridY);

                var tile = FindOverlayForPoint(lat, lon, cellSize, key, spatialIndex);
                if (tile != null)
                {
                    tiles.Add(tile);
                }
            }
        }

        var readOnlyTiles = tiles.ToArray();
        _cache.Set(cacheKey, readOnlyTiles, OverlayTilesCacheDuration);
        return readOnlyTiles;
    }

    /// <summary>Snaps a coordinate to the nearest grid boundary at the given cell size.</summary>
    private static double SnapToGrid(double value, double cellSize) =>
        Math.Floor(value / cellSize) * cellSize;

    private static MapOverlayTileDto? FindOverlayForPoint(
        double lat,
        double lon,
        double cellSize,
        (int, int) key,
        Dictionary<(int, int), List<(MapOverlayDto Dto, GeoUtils.ParsedGeometry Geometry)>> spatialIndex)
    {
        if (spatialIndex.TryGetValue(key, out var candidates))
        {
            // Simple point-in-polygon check for the center of the tile on candidates only
            var overlayIndex = candidates.FindIndex(o =>
                GeoUtils.IsPointInPolygon(lat, lon, o.Geometry));

            if (overlayIndex >= 0)
            {
                var overlay = candidates[overlayIndex];
                return new MapOverlayTileDto(
                    lat,
                    lon,
                    cellSize,
                    overlay.Dto.MetricValue,
                    overlay.Dto.DisplayValue
                );
            }
        }
        return null;
    }

    private async Task<List<MapOverlayDto>> CalculateAveragePriceOverlayAsync(
        double minLat, double minLon, double maxLat, double maxLon, CancellationToken ct)
    {
        // Round bounds to 2 decimal places (~1km grid) for a sensible cache granularity
        var cacheKey = FormattableString.Invariant(
            $"PriceOverlay_{Math.Round(minLat, 2)}_{Math.Round(minLon, 2)}_{Math.Round(maxLat, 2)}_{Math.Round(maxLon, 2)}");

        if (_cache.TryGetValue(cacheKey, out List<MapOverlayDto>? cached) && cached is not null)
        {
            _logger.LogDebug("Cache hit for price overlay, key={CacheKey}", cacheKey);
            return cached;
        }

        var overlaysTask = _cbsGeoClient.GetNeighborhoodOverlaysAsync(minLat, minLon, maxLat, maxLon, MapOverlayMetric.PopulationDensity, ct);
        var listingDataTask = _repository.GetListingsPriceDataAsync(minLat, minLon, maxLat, maxLon, ct);

        await Task.WhenAll(overlaysTask, listingDataTask);

        var overlays = await overlaysTask;
        var listingData = await listingDataTask;

        var result = overlays.Select(overlay =>
        {
            var geometry = GeoUtils.ParseGeometry(overlay.GeoJson);
            var neighborhoodListings = listingData.Where(l =>
                l.Latitude.HasValue && l.Longitude.HasValue &&
                GeoUtils.IsPointInPolygon(l.Latitude.Value, l.Longitude.Value, geometry));

            var avgPrice = CalculateAveragePrice(neighborhoodListings);

            var displayValue = avgPrice.HasValue ? $"€ {avgPrice:N0} / m²" : "No listing data";
            var metricValue = avgPrice ?? 0;

            return overlay with
            {
                MetricName = "PricePerSquareMeter",
                MetricValue = metricValue,
                DisplayValue = displayValue
            };
        }).ToList();

        _cache.Set(cacheKey, result, PriceOverlayCacheDuration);
        return result;
    }

    private static double? CalculateAveragePrice(IEnumerable<ListingPriceData> listings)
    {
        var validListings = listings
            .Where(l => l.Price.HasValue && l.LivingAreaM2.HasValue && l.LivingAreaM2.Value > 0)
            .ToList();

        if (validListings.Count == 0)
        {
            return null;
        }

        return (double?)validListings.Average(l => l.Price!.Value / l.LivingAreaM2!.Value);
    }

    private static double GetCellSize(double zoom)
    {
        if (zoom >= 13) return 0.005;
        if (zoom >= 11) return 0.01;
        if (zoom >= 9) return 0.02;
        if (zoom >= 7) return 0.05;
        return 0.1;
    }

    private static void ValidateAggregatedBoundingBox(double minLat, double minLon, double maxLat, double maxLon)
    {
        if (double.IsNaN(minLat) || double.IsNaN(minLon) || double.IsNaN(maxLat) || double.IsNaN(maxLon))
            throw new ValidationException("Coordinates must be valid numbers.");

        if (minLat >= maxLat)
            throw new ValidationException("Invalid bounding box dimensions.");

        var lonSpan = maxLon >= minLon ? maxLon - minLon : 360 - (minLon - maxLon);

        if (maxLat - minLat > MaxAggregatedSpan || lonSpan > MaxAggregatedSpan)
            throw new ValidationException($"Bounding box span too large for aggregated view. Max is {MaxAggregatedSpan}.");
    }
}
