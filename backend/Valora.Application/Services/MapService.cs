using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Utilities;
using Valora.Application.DTOs.Map;

namespace Valora.Application.Services;

public class MapService : IMapService
{
    // Removed IMapRepository dependency as it's no longer used for Listings
    private readonly IAmenityClient _amenityClient;
    private readonly ICbsGeoClient _cbsGeoClient;

    public MapService(
        IAmenityClient amenityClient,
        ICbsGeoClient cbsGeoClient)
    {
        _amenityClient = amenityClient;
        _cbsGeoClient = cbsGeoClient;
    }

    public Task<List<MapCityInsightDto>> GetCityInsightsAsync(CancellationToken cancellationToken = default)
    {
        // Listings have been removed, so we return an empty list or placeholder data.
        return Task.FromResult(new List<MapCityInsightDto>());
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
            // Price data was derived from Listings, which are now removed.
            return new List<MapOverlayDto>();
        }

        return await _cbsGeoClient.GetNeighborhoodOverlaysAsync(minLat, minLon, maxLat, maxLon, metric, cancellationToken);
    }
}
