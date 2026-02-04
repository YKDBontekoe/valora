using Valora.Application.Common.Models;
using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces;

public interface IListingService
{
    Task<PaginatedList<ListingDto>> GetAllAsync(ListingFilterDto filter, CancellationToken cancellationToken);
    Task<ListingDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
