using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IListingRepository
{
    Task<PaginatedList<Listing>> GetAllAsync(ListingFilterDto filter, CancellationToken cancellationToken = default);
    Task<Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Listing?> GetByFundaIdAsync(string fundaId, CancellationToken cancellationToken = default);
    Task<Listing> AddAsync(Listing listing, CancellationToken cancellationToken = default);
    Task UpdateAsync(Listing listing, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}
