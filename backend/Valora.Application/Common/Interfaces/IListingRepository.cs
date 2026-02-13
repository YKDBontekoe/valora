using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IListingRepository
{
    Task<PaginatedList<ListingDto>> GetAllAsync(ListingFilterDto filter, CancellationToken cancellationToken = default);
    Task<PaginatedList<ListingSummaryDto>> GetSummariesAsync(ListingFilterDto filter, CancellationToken cancellationToken = default);
    Task<Listing?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Listing?> GetByFundaIdAsync(string fundaId, CancellationToken cancellationToken = default);
    Task<List<Listing>> GetByFundaIdsAsync(IEnumerable<string> fundaIds, CancellationToken cancellationToken = default);
    Task<Listing> AddAsync(Listing listing, CancellationToken cancellationToken = default);
    Task UpdateAsync(Listing listing, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all listings that are currently active (not sold/withdrawn) for status updates.
    /// </summary>
    Task<List<Listing>> GetActiveListingsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrieves listings by city/region with basic filtering.
    /// Returns lightweight summary DTOs.
    /// </summary>
    Task<List<ListingSummaryDto>> GetByCityAsync(
        string city, 
        int? minPrice = null, 
        int? maxPrice = null, 
        int? minBedrooms = null,
        int pageSize = 20, 
        int page = 1,
        CancellationToken cancellationToken = default);
}
