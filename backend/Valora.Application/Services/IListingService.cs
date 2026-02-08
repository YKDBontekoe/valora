using Valora.Domain.Entities;

namespace Valora.Application.Services;

public record ProcessListingResult(Listing Listing, bool IsNew, bool IsUpdated);

public interface IListingService
{
    /// <summary>
    /// Processes a listing from a scraper or import source.
    /// Handles creation, updates (merging), and price history tracking.
    /// If existingListing is provided, it is used directly (optimization).
    /// If not provided, it will attempt to fetch it by FundaId.
    /// </summary>
    Task<ProcessListingResult> ProcessListingAsync(Listing newListing, Listing? existingListing = null, CancellationToken cancellationToken = default);
}
