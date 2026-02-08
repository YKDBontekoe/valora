using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IListingService
{
    Task<List<Listing>> GetListingsByFundaIdsAsync(List<string> fundaIds, CancellationToken cancellationToken);
    Task SaveListingAsync(Listing newListing, Listing? existingListing, bool shouldNotify, CancellationToken cancellationToken);
}
