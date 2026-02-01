using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class ListingQueryService : IListingQueryService
{
    private readonly IListingRepository _listingRepository;

    public ListingQueryService(IListingRepository listingRepository)
    {
        _listingRepository = listingRepository;
    }

    public Task<PaginatedList<ListingDto>> GetListingsAsync(ListingFilterDto filter, CancellationToken cancellationToken = default)
    {
        return _listingRepository.GetAllAsync(filter, cancellationToken);
    }

    public async Task<ListingDto?> GetListingByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var listing = await _listingRepository.GetByIdAsync(id, cancellationToken);
        return listing is null ? null : MapToDto(listing);
    }

    private static ListingDto MapToDto(Listing listing)
    {
        return new ListingDto(
            listing.Id,
            listing.FundaId,
            listing.Address,
            listing.City,
            listing.PostalCode,
            listing.Price,
            listing.Bedrooms,
            listing.Bathrooms,
            listing.LivingAreaM2,
            listing.PlotAreaM2,
            listing.PropertyType,
            listing.Status,
            listing.Url,
            listing.ImageUrl,
            listing.ListedDate,
            listing.CreatedAt
        );
    }
}
