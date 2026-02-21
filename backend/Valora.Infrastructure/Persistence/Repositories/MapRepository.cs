using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs.Map;

namespace Valora.Infrastructure.Persistence.Repositories;

public class MapRepository : IMapRepository
{
    private readonly ValoraDbContext _context;

    public MapRepository(ValoraDbContext context)
    {
        _context = context;
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

    public async Task<List<ListingPriceData>> GetListingsPriceDataAsync(
        double minLat, double minLon, double maxLat, double maxLon, CancellationToken cancellationToken = default)
    {
        return await _context.Listings
            .Where(l => l.Latitude >= minLat && l.Latitude <= maxLat &&
                        l.Longitude >= minLon && l.Longitude <= maxLon &&
                        l.Price.HasValue && l.LivingAreaM2.HasValue && l.LivingAreaM2 > 0)
            .Select(l => new ListingPriceData(l.Price, l.LivingAreaM2, l.Latitude, l.Longitude))
            .ToListAsync(cancellationToken);
    }
}
