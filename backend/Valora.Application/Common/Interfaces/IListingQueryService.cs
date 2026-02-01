using Valora.Application.Common.Models;
using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IListingQueryService
{
    Task<PaginatedList<ListingDto>> GetListingsAsync(ListingFilterDto filter, CancellationToken cancellationToken = default);
    Task<ListingDto?> GetListingByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
