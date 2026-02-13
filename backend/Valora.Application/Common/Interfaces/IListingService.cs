using Valora.Application.Common.Models;
using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IListingService
{
    Task<Result<PaginatedList<ListingSummaryDto>>> GetSummariesAsync(ListingFilterDto filter, CancellationToken ct = default);
    Task<Result<ListingDto>> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Result<double>> EnrichListingAsync(Guid id, CancellationToken ct = default);
}
