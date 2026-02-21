using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Utilities;
using Valora.Application.DTOs.Map;
using Valora.Application.DTOs.Property;

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

        var overlays = await _cbsGeoClient.GetNeighborhoodOverlaysAsync(
            minLat - cellSize,
            minLon - cellSize,
            maxLat + cellSize,
            maxLon + cellSize,
            metric,
            cancellationToken);

        var tiles = new List<MapOverlayTileDto>();

        for (double lat = minLat + cellSize / 2; lat < maxLat; lat += cellSize)
        {
            for (double lon = minLon + cellSize / 2; lon < maxLon; lon += cellSize)
            {
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

    public async Task<PropertyDetailDto?> GetPropertyDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var listing = await _repository.GetListingByIdAsync(id, cancellationToken);
        if (listing == null)
        {
            return null;
        }

        decimal? pricePerM2 = null;
        if (listing.Price.HasValue && listing.LivingAreaM2.HasValue && listing.LivingAreaM2 > 0)
        {
            pricePerM2 = listing.Price.Value / listing.LivingAreaM2.Value;
        }

        // Calculate Percentile
        double? pricePercentile = null;
        if (pricePerM2.HasValue && listing.Latitude.HasValue && listing.Longitude.HasValue)
        {
            // Fetch nearby listings (approx 2km radius)
            double range = 0.02;
            var nearbyListings = await _repository.GetListingsPriceDataAsync(
                listing.Latitude.Value - range,
                listing.Longitude.Value - range,
                listing.Latitude.Value + range,
                listing.Longitude.Value + range,
                cancellationToken);

            var validPrices = nearbyListings
                .Where(l => l.Price.HasValue && l.LivingAreaM2.HasValue && l.LivingAreaM2 > 0)
                .Select(l => l.Price!.Value / l.LivingAreaM2!.Value)
                .OrderBy(p => p)
                .ToList();

            if (validPrices.Count > 1) // Need at least 2 to have a percentile relative to others
            {
                int countLower = validPrices.Count(p => p < pricePerM2.Value);
                pricePercentile = (double)countLower / validPrices.Count * 100.0;
            }
        }

        // Fetch Amenities (small radius, e.g. 500m ~ 0.005 deg)
        List<MapAmenityDto> amenities = [];
        if (listing.Latitude.HasValue && listing.Longitude.HasValue)
        {
             double amenityRange = 0.005;
             amenities = await _amenityClient.GetAmenitiesInBboxAsync(
                 listing.Latitude.Value - amenityRange,
                 listing.Longitude.Value - amenityRange,
                 listing.Latitude.Value + amenityRange,
                 listing.Longitude.Value + amenityRange,
                 null,
                 cancellationToken);
        }

        return new PropertyDetailDto(
            listing.Id,
            listing.FundaId,
            listing.Price,
            listing.Address,
            listing.City,
            listing.PostalCode,
            listing.Bedrooms,
            listing.Bathrooms,
            listing.LivingAreaM2,
            listing.EnergyLabel,
            listing.Description,
            listing.ImageUrls,
            listing.ContextCompositeScore,
            listing.ContextSafetyScore,
            listing.ContextSocialScore,
            listing.ContextAmenitiesScore,
            listing.ContextEnvironmentScore,
            pricePerM2,
            listing.NeighborhoodAvgPriceM2,
            pricePercentile,
            amenities,
            listing.Latitude,
            listing.Longitude
        );
    }

    public async Task<List<MapPropertyDto>> GetMapPropertiesAsync(double minLat, double minLon, double maxLat, double maxLon, CancellationToken cancellationToken = default)
    {
        GeoUtils.ValidateBoundingBox(minLat, minLon, maxLat, maxLon);
        // Limit zoom level? Assuming repository handles basic filtering or caller validates zoom level
        return await _repository.GetMapPropertiesAsync(minLat, minLon, maxLat, maxLon, cancellationToken);
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
