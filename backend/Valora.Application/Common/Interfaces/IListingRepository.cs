using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IListingRepository
{
    Task<PaginatedList<ListingDto>> GetAllAsync(ListingFilterDto filter, CancellationToken cancellationToken = default);
    Task<Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Listing?> GetByFundaIdAsync(string fundaId, CancellationToken cancellationToken = default);
    Task<Listing> AddAsync(Listing listing, CancellationToken cancellationToken = default);
    Task UpdateAsync(Listing listing, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all listings that are currently active (not sold/withdrawn) for status updates.
    /// </summary>
    Task<List<Listing>> GetActiveListingsAsync(CancellationToken cancellationToken = default);
}
