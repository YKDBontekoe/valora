using Valora.Application.DTOs.Listings;

namespace Valora.Application.Common.Interfaces.Listings;

public interface IListingService
{
    Task<List<ListingDto>> SearchListingsAsync(ListingSearchRequest request, CancellationToken ct = default);
}
