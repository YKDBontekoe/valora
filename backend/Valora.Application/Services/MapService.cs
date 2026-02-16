using Valora.Application.Common.Exceptions;
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
        ValidateBbox(minLat, minLon, maxLat, maxLon);
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
        ValidateBbox(minLat, minLon, maxLat, maxLon);
        if (metric == MapOverlayMetric.PricePerSquareMeter)
        {
            return await CalculateAveragePriceOverlayAsync(minLat, minLon, maxLat, maxLon, cancellationToken);
        }

        return await _cbsGeoClient.GetNeighborhoodOverlaysAsync(minLat, minLon, maxLat, maxLon, metric, cancellationToken);
    }

    private void ValidateBbox(double minLat, double minLon, double maxLat, double maxLon)
    {
        if (minLat < -90 || minLat > 90 || maxLat < -90 || maxLat > 90)
        {
            throw new ValidationException("Latitudes must be between -90 and 90.");
        }

        if (minLon < -180 || minLon > 180 || maxLon < -180 || maxLon > 180)
        {
            throw new ValidationException("Longitudes must be between -180 and 180.");
        }

        if (minLat >= maxLat)
        {
            throw new ValidationException("minLat must be less than maxLat.");
        }

        if (minLon >= maxLon)
        {
            throw new ValidationException("minLon must be less than maxLon.");
        }

        // Limit bbox size to prevent heavy queries (max 0.5 deg span in each direction)
        const double maxSpan = 0.5;
        if (maxLat - minLat > maxSpan || maxLon - minLon > maxSpan)
        {
            throw new ValidationException($"Bounding box span too large. Maximum allowed is {maxSpan} degrees.");
        }
    }

    private async Task<List<MapOverlayDto>> CalculateAveragePriceOverlayAsync(
        double minLat, double minLon, double maxLat, double maxLon, CancellationToken ct)
    {
        // Fetch boundaries first
        var overlays = await _cbsGeoClient.GetNeighborhoodOverlaysAsync(minLat, minLon, maxLat, maxLon, MapOverlayMetric.PopulationDensity, ct);

        // Query listing data via repository
        var listingData = await _repository.GetListingsPriceDataAsync(minLat, minLon, maxLat, maxLon, ct);

        var validListings = GetValidListings(listingData);
        double? avgPrice = null;

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

    private static List<ListingPriceData> GetValidListings(IEnumerable<ListingPriceData> listings)
    {
        return listings
            .Where(l => l.Price.HasValue && l.LivingAreaM2.HasValue && l.LivingAreaM2.Value > 0)
            .ToList();
    }
}
