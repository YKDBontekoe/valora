using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Utilities;
using Valora.Application.DTOs.Map;
using Microsoft.Extensions.Caching.Memory;

namespace Valora.Application.Services;

public class MapService : IMapService
{
    private readonly IMapRepository _repository;
    private readonly IAmenityClient _amenityClient;
    private readonly ICbsGeoClient _cbsGeoClient;
    private readonly IMemoryCache _cache;
    private const double MaxAggregatedSpan = 2.0; // Larger span for aggregated views

    public MapService(
        IMapRepository repository,
        IAmenityClient amenityClient,
        ICbsGeoClient cbsGeoClient,
        IMemoryCache cache)
    {
        _repository = repository;
        _amenityClient = amenityClient;
        _cbsGeoClient = cbsGeoClient;
        _cache = cache;
    }

    public async Task<List<MapCityInsightDto>> GetCityInsightsAsync(CancellationToken cancellationToken = default)
    {
        return await _repository.GetCityInsightsAsync(cancellationToken);
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
            .Select(g =>
            {
                var count = g.Count();
                var lat = (g.Key.Lat * cellSize) + (cellSize / 2);
                var lon = (g.Key.Lon * cellSize) + (cellSize / 2);

                var typeCounts = g.GroupBy(a => a.Type)
                    .ToDictionary(tg => tg.Key, tg => tg.Count());

                return new MapAmenityClusterDto(lat, lon, count, typeCounts);
            })
            .ToList();

        return clusters;
    }

    public async Task<List<MapOverlayTileDto>> GetMapOverlayTilesAsync(
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

        var cacheKey = FormattableString.Invariant($"MapOverlayTiles_{Math.Round(minLat, 4)}_{Math.Round(minLon, 4)}_{Math.Round(maxLat, 4)}_{Math.Round(maxLon, 4)}_{zoom}_{metric}");
        if (_cache.TryGetValue(cacheKey, out List<MapOverlayTileDto>? cachedTiles))
        {
            return cachedTiles!;
        }


        // Fetch detailed overlays
        var overlays = await _cbsGeoClient.GetNeighborhoodOverlaysAsync(
            minLat - cellSize,
            minLon - cellSize,
            maxLat + cellSize,
            maxLon + cellSize,
            metric,
            cancellationToken);

        var tiles = new List<MapOverlayTileDto>();

        // Pre-parse geometries for performance
        var parsedOverlays = overlays.Select(o =>
            (Dto: o, Geometry: GeoUtils.ParseGeometry(o.GeoJson))
        ).ToList();

        // Build spatial index
        double indexCellSize = cellSize * 5; // e.g., 5x tile size for the index grid
        var spatialIndex = new Dictionary<(int, int), List<(MapOverlayDto Dto, GeoUtils.ParsedGeometry Geometry)>>();

        foreach (var po in parsedOverlays)
        {
            int minX = (int)Math.Floor(po.Geometry.BBox.MinLon / indexCellSize);
            int maxX = (int)Math.Floor(po.Geometry.BBox.MaxLon / indexCellSize);
            int minY = (int)Math.Floor(po.Geometry.BBox.MinLat / indexCellSize);
            int maxY = (int)Math.Floor(po.Geometry.BBox.MaxLat / indexCellSize);

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
                    list.Add(po);
                }
            }
        }

        // Rasterize into grid
        // Iterate by half cell size to center the points
        for (double lat = minLat + cellSize / 2; lat < maxLat; lat += cellSize)
        {
            int gridY = (int)Math.Floor(lat / indexCellSize);

            for (double lon = minLon + cellSize / 2; lon < maxLon; lon += cellSize)
            {
                int gridX = (int)Math.Floor(lon / indexCellSize);
                var key = (gridX, gridY);

                if (spatialIndex.TryGetValue(key, out var candidates))
                {
                    // Simple point-in-polygon check for the center of the tile on candidates only
                    var overlayIndex = candidates.FindIndex(o =>
                        GeoUtils.IsPointInPolygon(lat, lon, o.Geometry));

                    if (overlayIndex >= 0)
                    {
                        var overlay = candidates[overlayIndex];
                        tiles.Add(new MapOverlayTileDto(
                            lat,
                            lon,
                            cellSize,
                            overlay.Dto.MetricValue,
                            overlay.Dto.DisplayValue
                        ));
                    }
                }
            }
        }

        _cache.Set(cacheKey, tiles, TimeSpan.FromMinutes(10));
        return tiles;
    }

    private async Task<List<MapOverlayDto>> CalculateAveragePriceOverlayAsync(
        double minLat, double minLon, double maxLat, double maxLon, CancellationToken ct)
    {
        var overlaysTask = _cbsGeoClient.GetNeighborhoodOverlaysAsync(minLat, minLon, maxLat, maxLon, MapOverlayMetric.PopulationDensity, ct);
        var listingDataTask = _repository.GetListingsPriceDataAsync(minLat, minLon, maxLat, maxLon, ct);

        await Task.WhenAll(overlaysTask, listingDataTask);

        var overlays = await overlaysTask;
        var listingData = await listingDataTask;

        return overlays.Select(overlay =>
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
