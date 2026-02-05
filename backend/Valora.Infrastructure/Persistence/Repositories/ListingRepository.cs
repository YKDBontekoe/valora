using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence.Extensions;

namespace Valora.Infrastructure.Persistence.Repositories;

public class ListingRepository : IListingRepository
{
    private readonly ValoraDbContext _context;

    public ListingRepository(ValoraDbContext context)
    {
        _context = context;
    }

    public async Task<PaginatedList<ListingDto>> GetAllAsync(ListingFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Listings.AsNoTracking().AsQueryable();
        var isPostgres = _context.Database.ProviderName?.Contains("PostgreSQL") == true;

        query = ApplyFilters(query, filter, isPostgres);

        var dtoQuery = query.Select(l => new ListingDto(
            l.Id,
            l.FundaId,
            l.Address,
            l.City,
            l.PostalCode,
            l.Price,
            l.Bedrooms,
            l.Bathrooms,
            l.LivingAreaM2,
            l.PlotAreaM2,
            l.PropertyType,
            l.Status,
            l.Url,
            l.ImageUrl,
            l.ListedDate,
            l.CreatedAt,
            // Rich Data
            l.Description, l.EnergyLabel, l.YearBuilt, l.ImageUrls,
            // Phase 2
            l.OwnershipType, l.CadastralDesignation, l.VVEContribution, l.HeatingType,
            l.InsulationType, l.GardenOrientation, l.HasGarage, l.ParkingType,
            // Phase 3
            l.AgentName, l.VolumeM3, l.BalconyM2, l.GardenM2, l.ExternalStorageM2,
            l.Features,
            // Geo & Media
            l.Latitude, l.Longitude, l.VideoUrl, l.VirtualTourUrl, l.FloorPlanUrls, l.BrochureUrl,
            // Construction
            l.RoofType, l.NumberOfFloors, l.ConstructionPeriod, l.CVBoilerBrand, l.CVBoilerYear,
            // Broker
            l.BrokerPhone, l.BrokerLogoUrl,
            // Infra
            l.FiberAvailable,
            // Status
            l.PublicationDate, l.IsSoldOrRented, l.Labels
        ));

        return await dtoQuery.ToPaginatedListAsync(filter.Page ?? 1, filter.PageSize ?? 10);
    }

    public async Task<PaginatedList<ListingSummaryDto>> GetSummariesAsync(ListingFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Listings.AsNoTracking().AsQueryable();
        var isPostgres = _context.Database.ProviderName?.Contains("PostgreSQL") == true;

        query = ApplyFilters(query, filter, isPostgres);

        var dtoQuery = query.Select(l => new ListingSummaryDto(
            l.Id,
            l.FundaId,
            l.Address,
            l.City,
            l.PostalCode,
            l.Price,
            l.Bedrooms,
            l.Bathrooms,
            l.LivingAreaM2,
            l.PlotAreaM2,
            l.PropertyType,
            l.Status,
            l.Url,
            l.ImageUrl,
            l.ListedDate,
            l.CreatedAt,
            l.EnergyLabel,
            l.IsSoldOrRented,
            l.Labels
        ));

        return await dtoQuery.ToPaginatedListAsync(filter.Page ?? 1, filter.PageSize ?? 10);
    }

    private IQueryable<Listing> ApplyFilters(IQueryable<Listing> query, ListingFilterDto filter, bool isPostgres)
    {
        query = query.ApplySearchFilter(filter, isPostgres);

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(l => l.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(l => l.Price <= filter.MaxPrice.Value);
        }

        query = query.ApplyCityFilter(filter.City, isPostgres);

        if (filter.MinBedrooms.HasValue)
        {
            query = query.Where(l => l.Bedrooms >= filter.MinBedrooms.Value);
        }

        if (filter.MinLivingArea.HasValue)
        {
            query = query.Where(l => l.LivingAreaM2 >= filter.MinLivingArea.Value);
        }

        if (filter.MaxLivingArea.HasValue)
        {
            query = query.Where(l => l.LivingAreaM2 <= filter.MaxLivingArea.Value);
        }

        query = query.ApplySorting(filter.SortBy, filter.SortOrder);

        return query;
    }

    public async Task<Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Listings
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<Listing?> GetByFundaIdAsync(string fundaId, CancellationToken cancellationToken = default)
    {
        return await _context.Listings
            .FirstOrDefaultAsync(l => l.FundaId == fundaId, cancellationToken);
    }

    public async Task<List<Listing>> GetByFundaIdsAsync(IEnumerable<string> fundaIds, CancellationToken cancellationToken = default)
    {
        return await _context.Listings
            .Where(l => fundaIds.Contains(l.FundaId))
            .ToListAsync(cancellationToken);
    }

    public async Task<Listing> AddAsync(Listing listing, CancellationToken cancellationToken = default)
    {
        _context.Listings.Add(listing);
        await _context.SaveChangesAsync(cancellationToken);
        return listing;
    }

    public async Task UpdateAsync(Listing listing, CancellationToken cancellationToken = default)
    {
        listing.UpdatedAt = DateTime.UtcNow;
        _context.Listings.Update(listing);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var listing = await _context.Listings.FindAsync([id], cancellationToken);
        if (listing != null)
        {
            _context.Listings.Remove(listing);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Listings.CountAsync(cancellationToken);
    }

    public async Task<List<Listing>> GetActiveListingsAsync(CancellationToken cancellationToken = default)
    {
        // Return listings that are not explicitly sold or withdrawn
        // This covers "Beschikbaar", "Onder bod", "Onder optie", etc.
        return await _context.Listings
            .Where(l => l.Status != "Verkocht" && l.Status != "Ingetrokken" && !l.IsSoldOrRented)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Listing>> GetByCityAsync(
        string city, 
        int? minPrice = null, 
        int? maxPrice = null, 
        int? minBedrooms = null,
        int pageSize = 20, 
        int page = 1,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        var isPostgres = _context.Database.ProviderName?.Contains("PostgreSQL") == true;
        var query = _context.Listings.AsNoTracking().AsQueryable();

        // Filter by city (case-insensitive)
        query = query.ApplyCityFilter(city, isPostgres);

        // Apply filters
        if (minPrice.HasValue)
        {
            query = query.Where(l => l.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(l => l.Price <= maxPrice.Value);
        }

        if (minBedrooms.HasValue)
        {
            query = query.Where(l => l.Bedrooms >= minBedrooms.Value);
        }

        // Order by last fetch time (freshest first), then by price
        query = query
            .OrderByDescending(l => l.LastFundaFetchUtc)
            .ThenByDescending(l => l.Price);

        // Paginate
        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
