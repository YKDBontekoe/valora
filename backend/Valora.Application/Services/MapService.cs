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

    private async Task<List<MapOverlayDto>> CalculateAveragePriceOverlayAsync(
        double minLat, double minLon, double maxLat, double maxLon, CancellationToken ct)
    {
        // Fetch boundaries and listing data concurrently
        var overlaysTask = _cbsGeoClient.GetNeighborhoodOverlaysAsync(minLat, minLon, maxLat, maxLon, MapOverlayMetric.PopulationDensity, ct);
        var listingDataTask = _repository.GetListingsPriceDataAsync(minLat, minLon, maxLat, maxLon, ct);

        await Task.WhenAll(overlaysTask, listingDataTask);

        var overlays = await overlaysTask;
        var listingData = await listingDataTask;

        var avgPrice = CalculateAveragePrice(listingData);

        return CreatePriceOverlays(overlays, avgPrice);
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

    private static List<MapOverlayDto> CreatePriceOverlays(IEnumerable<MapOverlayDto> overlays, double? avgPrice)
    {
        var displayValue = avgPrice.HasValue ? $"€ {avgPrice:N0} / m²" : "No listing data";
        var metricValue = avgPrice ?? 0;

        return overlays.Select(overlay => overlay with
        {
            MetricName = "PricePerSquareMeter",
            MetricValue = metricValue,
            DisplayValue = displayValue
        }).ToList();
    }
}
