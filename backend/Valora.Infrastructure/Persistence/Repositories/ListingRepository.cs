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

    /// <summary>
    /// Retrieves a paginated list of full listing DTOs based on the provided filter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Performance Optimization:</strong>
    /// This method uses <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}"/> to bypass the EF Core ChangeTracker.
    /// This significantly reduces memory overhead and CPU usage for read-only scenarios.
    /// </para>
    /// <para>
    /// <strong>Projection:</strong>
    /// It projects directly to <see cref="ListingDto"/> in the database query. This prevents "Select N+1" issues
    /// and ensures only the necessary columns are fetched from the database.
    /// </para>
    /// </remarks>
    public async Task<PaginatedList<ListingDto>> GetAllAsync(ListingFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Listings.AsNoTracking().AsQueryable();
        var isPostgres = _context.Database.ProviderName?.Contains("PostgreSQL") == true;

        query = ApplyFilters(query, filter, isPostgres);

        var dtoQuery = query.Select(ListingProjections.ToDto);

        return await dtoQuery.ToPaginatedListAsync(filter.Page ?? 1, filter.PageSize ?? 10, cancellationToken);
    }

    /// <summary>
    /// Retrieves a paginated list of lightweight listing summaries.
    /// </summary>
    /// <remarks>
    /// Similar to <see cref="GetAllAsync"/>, this method uses <see cref="EntityFrameworkQueryableExtensions.AsNoTracking{TEntity}"/>
    /// and projects to <see cref="ListingSummaryDto"/> to minimize data transfer for list views.
    /// </remarks>
    public async Task<PaginatedList<ListingSummaryDto>> GetSummariesAsync(ListingFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Listings.AsNoTracking().AsQueryable();
        var isPostgres = _context.Database.ProviderName?.Contains("PostgreSQL") == true;

        query = ApplyFilters(query, filter, isPostgres);

        var dtoQuery = query.Select(ListingProjections.ToSummaryDto);

        return await dtoQuery.ToPaginatedListAsync(filter.Page ?? 1, filter.PageSize ?? 10, cancellationToken);
    }

    /// <summary>
    /// Applies dynamic filtering based on the provided criteria.
    /// </summary>
    /// <remarks>
    /// This method builds the IQueryable expression tree step-by-step.
    /// The database provider (EF Core) translates this into a single optimized SQL WHERE clause.
    /// Helper methods like ApplySearchFilter handle provider-specific logic (e.g., ILIKE for Postgres vs LIKE for SQL Server).
    /// </remarks>
    private IQueryable<Listing> ApplyFilters(IQueryable<Listing> query, ListingFilterDto filter, bool isPostgres)
    {
        // Filter out inactive listings
        query = query.WhereActive();

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

        if (filter.MinSafetyScore.HasValue)
        {
            query = query.Where(l => l.ContextSafetyScore >= filter.MinSafetyScore.Value);
        }

        if (filter.MinCompositeScore.HasValue)
        {
            query = query.Where(l => l.ContextCompositeScore >= filter.MinCompositeScore.Value);
        }

        query = query.ApplySorting(filter.SortBy, filter.SortOrder);

        return query;
    }

    public async Task<Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Listings
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
    }

    public async Task<Listing?> GetByIdAsNoTrackingAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Listings
            .AsNoTracking()
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

    /// <summary>
    /// Gets a list of all active listings without tracking changes.
    /// Entities returned by this method are detached and modifications will not be persisted unless explicitly updated.
    /// </summary>
    /// <returns>A list of active listings (detached).</returns>
    public async Task<List<Listing>> GetActiveListingsAsync(CancellationToken cancellationToken = default)
    {
        // Return listings that are not explicitly sold or withdrawn
        // This covers "Beschikbaar", "Onder bod", "Onder optie", etc.
        return await _context.Listings.AsNoTracking()
            .WhereActive()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ListingSummaryDto>> GetByCityAsync(
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
        var query = _context.Listings.AsNoTracking().AsQueryable()
            .WhereActive();

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

        // Paginate and Project
        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ListingProjections.ToSummaryDto)
            .ToListAsync(cancellationToken);
    }
}
