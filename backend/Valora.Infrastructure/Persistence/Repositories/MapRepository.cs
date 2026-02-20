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

    // Removed GetCityInsightsAsync and GetListingsPriceDataAsync as they depended on Listing entity
}
