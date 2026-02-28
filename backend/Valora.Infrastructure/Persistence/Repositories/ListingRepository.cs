using Microsoft.EntityFrameworkCore;
using Valora.Application.Common.Interfaces.Listings;
using Valora.Application.DTOs.Listings;

namespace Valora.Infrastructure.Persistence.Repositories;

public class ListingRepository : IListingRepository
{
    private readonly ValoraDbContext _context;

    public ListingRepository(ValoraDbContext context)
    {
        _context = context;
    }

    public async Task<List<ListingDto>> SearchListingsAsync(ListingSearchRequest request, CancellationToken ct = default)
    {
        var query = _context.Listings.AsQueryable();

        // Location Filters
        if (!string.IsNullOrWhiteSpace(request.City))
        {
            query = query.Where(l => l.City == request.City);
        }

        if (!string.IsNullOrWhiteSpace(request.PostalCode))
        {
            query = query.Where(l => l.PostalCode != null && l.PostalCode.StartsWith(request.PostalCode));
        }

        // Bounding Box
        if (request.MinLat.HasValue && request.MinLon.HasValue && request.MaxLat.HasValue && request.MaxLon.HasValue)
        {
            query = query.Where(l =>
                l.Latitude >= request.MinLat.Value && l.Latitude <= request.MaxLat.Value &&
                l.Longitude >= request.MinLon.Value && l.Longitude <= request.MaxLon.Value);
        }

        // Numeric Filters
        if (request.MinPrice.HasValue)
        {
            query = query.Where(l => l.Price >= request.MinPrice.Value);
        }

        if (request.MaxPrice.HasValue)
        {
            query = query.Where(l => l.Price <= request.MaxPrice.Value);
        }

        if (request.MinArea.HasValue)
        {
            query = query.Where(l => l.LivingAreaM2 >= request.MinArea.Value);
        }

        // Categorical Filters
        if (!string.IsNullOrWhiteSpace(request.PropertyType))
        {
            query = query.Where(l => l.PropertyType == request.PropertyType);
        }

        if (!string.IsNullOrWhiteSpace(request.EnergyLabel))
        {
            query = query.Where(l => l.EnergyLabel == request.EnergyLabel);
        }

        // Year Built
        if (request.MinYearBuilt.HasValue)
        {
            query = query.Where(l => l.YearBuilt >= request.MinYearBuilt.Value);
        }

        if (request.MaxYearBuilt.HasValue)
        {
            query = query.Where(l => l.YearBuilt <= request.MaxYearBuilt.Value);
        }

        // Sorting
        query = request.SortBy?.ToLower() switch
        {
            "relevance" => query.OrderByDescending(l => l.ContextCompositeScore), // Approximation for relevance
            "newest" => query.OrderByDescending(l => l.ListedDate),
            "price" => query.OrderBy(l => l.Price),
            "pricepersqm" => query.OrderBy(l => l.LivingAreaM2 != null && l.LivingAreaM2 > 0 ? l.Price / l.LivingAreaM2 : decimal.MaxValue),
            "commute" => query.OrderByDescending(l => l.ContextCompositeScore), // Needs separate implementation if commute score exists
            _ => query.OrderByDescending(l => l.ListedDate)
        };

        // Pagination
        var skip = (request.Page - 1) * request.PageSize;
        var take = request.PageSize;

        var listings = await query.Skip(skip).Take(take).ToListAsync(ct);

        return listings.Select(l => new ListingDto(
            l.Id,
            l.FundaId,
            l.Address,
            l.City,
            l.PostalCode,
            l.Price,
            l.Bedrooms,
            l.Bathrooms,
            l.LivingAreaM2,
            l.PropertyType,
            l.Status,
            l.ImageUrl,
            l.ListedDate,
            l.EnergyLabel,
            l.YearBuilt,
            l.Latitude,
            l.Longitude,
            l.ContextCompositeScore
        )).ToList();
    }
}
