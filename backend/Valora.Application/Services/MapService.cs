using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Utilities;
using Valora.Application.DTOs.Map;

namespace Valora.Application.Services;

public class MapService : IMapService
{
    private readonly IMapRepository _repository;
    private readonly IAmenityClient _amenityClient;
    private readonly ICbsGeoClient _cbsGeoClient;
    private const double MaxAggregatedSpan = 2.0; // Larger span for aggregated views

    public MapService(
        IMapRepository repository,
        IAmenityClient amenityClient,
        ICbsGeoClient cbsGeoClient)
    {
        _repository = repository;
        _amenityClient = amenityClient;
        _cbsGeoClient = cbsGeoClient;
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
        double cellSize = GetCellSize(zoom);

        // Fetch detailed overlays
        var overlays = await _cbsGeoClient.GetNeighborhoodOverlaysAsync(
            minLat - cellSize,
            minLon - cellSize,
            maxLat + cellSize,
            maxLon + cellSize,
            metric,
            cancellationToken);

        var tiles = new List<MapOverlayTileDto>();

        // Rasterize into grid
        // Iterate by half cell size to center the points
        for (double lat = minLat + cellSize / 2; lat < maxLat; lat += cellSize)
        {
            for (double lon = minLon + cellSize / 2; lon < maxLon; lon += cellSize)
            {
                // Simple point-in-polygon check for the center of the tile
                var overlay = overlays.FirstOrDefault(o =>
                    GeoUtils.IsPointInPolygon(lat, lon, o.GeoJson));

                if (overlay != null)
                {
                    tiles.Add(new MapOverlayTileDto(
                        lat,
                        lon,
                        cellSize,
                        overlay.MetricValue,
                        overlay.DisplayValue
                    ));
                }
            }
        }

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
            var neighborhoodListings = listingData.Where(l =>
                l.Latitude.HasValue && l.Longitude.HasValue &&
                GeoUtils.IsPointInPolygon(l.Latitude.Value, l.Longitude.Value, overlay.GeoJson));

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

        if (minLat >= maxLat || minLon >= maxLon)
            throw new ValidationException("Invalid bounding box dimensions.");

        if (maxLat - minLat > MaxAggregatedSpan || maxLon - minLon > MaxAggregatedSpan)
            throw new ValidationException($"Bounding box span too large for aggregated view. Max is {MaxAggregatedSpan}.");
    }
}
