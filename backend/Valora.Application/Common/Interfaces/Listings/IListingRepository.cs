using Valora.Application.DTOs.Listings;

namespace Valora.Application.Common.Interfaces.Listings;

public interface IListingRepository
{
    Task<List<ListingDto>> SearchListingsAsync(ListingSearchRequest request, CancellationToken ct = default);
}
