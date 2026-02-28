using Valora.Application.Common.Interfaces.Listings;
using Valora.Application.DTOs.Listings;

namespace Valora.Application.Services.Listings;

public class ListingService : IListingService
{
    private readonly IListingRepository _repository;

    public ListingService(IListingRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<ListingDto>> SearchListingsAsync(ListingSearchRequest request, CancellationToken ct = default)
    {
        return await _repository.SearchListingsAsync(request, ct);
    }
}
