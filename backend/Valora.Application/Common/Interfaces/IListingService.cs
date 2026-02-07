using Valora.Domain.Entities;

namespace Valora.Application.Common.Interfaces;

public interface IListingService
{
    /// <summary>
    /// Creates a new listing, initializes its price history, and persists it.
    /// </summary>
    Task CreateListingAsync(Listing listing, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing listing, checks for price changes to record history, and persists the update.
    /// </summary>
    Task UpdateListingAsync(Listing existingListing, Listing newListing, CancellationToken cancellationToken = default);
}
