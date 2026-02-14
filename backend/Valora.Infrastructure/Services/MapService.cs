using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs.Map;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Services;

public class MapService : IMapService
{
    private readonly ValoraDbContext _context;
    private readonly IAmenityClient _amenityClient;
    private readonly ICbsGeoClient _cbsGeoClient;

    public MapService(
        ValoraDbContext context,
        IAmenityClient amenityClient,
        ICbsGeoClient cbsGeoClient)
    {
        _context = context;
        _amenityClient = amenityClient;
        _cbsGeoClient = cbsGeoClient;
    }

    public async Task<List<MapCityInsightDto>> GetCityInsightsAsync(CancellationToken cancellationToken = default)
    {
        var query = _context.Listings
            .Where(x => x.City != null && x.Latitude.HasValue && x.Longitude.HasValue)
            .GroupBy(x => x.City!)
            .Select(g => new MapCityInsightDto(
                g.Key,
                g.Count(),
                g.Average(x => x.Latitude!.Value),
                g.Average(x => x.Longitude!.Value),
                g.Average(x => x.ContextCompositeScore),
                g.Average(x => x.ContextSafetyScore),
                g.Average(x => x.ContextSocialScore),
                g.Average(x => x.ContextAmenitiesScore)
            ));

        return await query.ToListAsync(cancellationToken);
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

        // Fix N+1: Query all relevant listings in the bbox once
        var listingsInBbox = await _context.Listings
            .Where(l => l.Latitude >= minLat && l.Latitude <= maxLat &&
                        l.Longitude >= minLon && l.Longitude <= maxLon &&
                        l.Price.HasValue && l.LivingAreaM2.HasValue && l.LivingAreaM2 > 0)
            .Select(l => new { l.Price, l.LivingAreaM2 })
            .ToListAsync(ct);

        double? avgPrice = null;
        if (listingsInBbox.Any())
        {
            avgPrice = (double)listingsInBbox.Average(l => l.Price!.Value / l.LivingAreaM2!.Value);
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
