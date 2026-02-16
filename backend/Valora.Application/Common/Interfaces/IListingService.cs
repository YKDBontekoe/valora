using Valora.Application.Common.Models;
using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IListingService
{
    Task<PaginatedList<ListingSummaryDto>> GetListingsAsync(ListingFilterDto filter, CancellationToken cancellationToken = default);
    Task<ListingDto?> GetListingByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ListingDto?> GetPdokListingAsync(string externalId, CancellationToken cancellationToken = default);
    Task<double> EnrichListingAsync(Guid id, CancellationToken cancellationToken = default);
}
