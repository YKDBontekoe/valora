using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IListingService
{
    Task<ListingDetailDto> GetListingDetailAsync(Guid listingId, CancellationToken ct = default);
}
