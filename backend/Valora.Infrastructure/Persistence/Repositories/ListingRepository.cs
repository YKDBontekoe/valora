using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

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

        // Filtering
        if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
        {
            if (isPostgres)
            {
                var escapedTerm = EscapeLikePattern(filter.SearchTerm);
                var search = $"%{escapedTerm}%";
                query = query.Where(l =>
                    EF.Functions.ILike(l.Address, search, @"\") ||
                    (l.City != null && EF.Functions.ILike(l.City, search, @"\")) ||
                    (l.PostalCode != null && EF.Functions.ILike(l.PostalCode, search, @"\")));
            }
            else
            {
                var search = filter.SearchTerm.ToLower();
                query = query.Where(l =>
                    l.Address.ToLower().Contains(search) ||
                    (l.City != null && l.City.ToLower().Contains(search)) ||
                    (l.PostalCode != null && l.PostalCode.ToLower().Contains(search)));
            }
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(l => l.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(l => l.Price <= filter.MaxPrice.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.City))
        {
            if (isPostgres)
            {
                var escapedCity = EscapeLikePattern(filter.City);
                query = query.Where(l => l.City != null && EF.Functions.ILike(l.City, escapedCity, @"\"));
            }
            else
            {
                query = query.Where(l => l.City != null && l.City.ToLower() == filter.City.ToLower());
            }
        }

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

        // Sorting
        query = (filter.SortBy?.ToLower(), filter.SortOrder?.ToLower()) switch
        {
            ("price", "desc") => query.OrderByDescending(l => l.Price),
            ("price", "asc") => query.OrderBy(l => l.Price),
            ("date", "asc") => query.OrderBy(l => l.ListedDate),
            ("date", "desc") => query.OrderByDescending(l => l.ListedDate),
            ("livingarea", "asc") => query.OrderBy(l => l.LivingAreaM2),
            ("livingarea", "desc") => query.OrderByDescending(l => l.LivingAreaM2),
            _ => query.OrderByDescending(l => l.ListedDate) // Default sort by date desc
        };

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
            l.CreatedAt
        ));

        return await PaginatedList<ListingDto>.CreateAsync(dtoQuery, filter.Page ?? 1, filter.PageSize ?? 10);
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
            .Where(l => l.Status != "Verkocht" && l.Status != "Ingetrokken")
            .ToListAsync(cancellationToken);
    }

    private static string EscapeLikePattern(string pattern)
    {
        return pattern
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_");
    }
}
