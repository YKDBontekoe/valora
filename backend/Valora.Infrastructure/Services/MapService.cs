using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs.Map;
using Valora.Infrastructure.Persistence;

namespace Valora.Infrastructure.Services;

public class MapService : IMapService
{
    private readonly ValoraDbContext _context;

    public MapService(ValoraDbContext context)
    {
        _context = context;
    }

    public async Task<List<MapCityInsightDto>> GetCityInsightsAsync(CancellationToken cancellationToken = default)
    {
        // Group listings by City to calculate aggregated metrics.
        // We only consider listings that have a valid City and coordinates.
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
}
