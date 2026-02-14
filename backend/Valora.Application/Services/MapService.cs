using Valora.Application.Common.Interfaces;
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
        if (metric == MapOverlayMetric.PricePerSquareMeter)
        {
            return await GetPricePerSquareMeterOverlaysAsync(minLat, minLon, maxLat, maxLon, cancellationToken);
        }

        return await _cbsGeoClient.GetNeighborhoodOverlaysAsync(minLat, minLon, maxLat, maxLon, metric, cancellationToken);
    }

    private async Task<List<MapOverlayDto>> GetPricePerSquareMeterOverlaysAsync(
        double minLat, double minLon, double maxLat, double maxLon, CancellationToken ct)
    {
        // Fetch boundaries first
        var overlays = await _cbsGeoClient.GetNeighborhoodOverlaysAsync(minLat, minLon, maxLat, maxLon, MapOverlayMetric.PopulationDensity, ct);

        // Query listing data via repository
        var listingData = await _repository.GetListingsPriceDataAsync(minLat, minLon, maxLat, maxLon, ct);

        double? avgPrice = null;
        var validListings = listingData
            .Where(l => l.Price.HasValue && l.LivingAreaM2.HasValue && l.LivingAreaM2.Value > 0)
            .ToList();

        if (validListings.Any())
        {
            avgPrice = (double)validListings.Average(l => l.Price!.Value / l.LivingAreaM2!.Value);
        }

        var results = new List<MapOverlayDto>();
        foreach (var overlay in overlays)
        {
            results.Add(overlay with {
                MetricName = "PricePerSquareMeter",
                MetricValue = avgPrice ?? 0,
                DisplayValue = avgPrice.HasValue ? $"€ {avgPrice:N0} / m²" : "No listing data"
            });
        }

        return results;
    }
}
