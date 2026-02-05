using Valora.Domain.Entities;

namespace Valora.Application.Scraping.Interfaces;

public interface IFundaApiClient
{
    Task<List<Listing>> SearchBuyAsync(string region, int page = 1, int? minPrice = null, int? maxPrice = null, CancellationToken cancellationToken = default);
    Task<List<Listing>> SearchRentAsync(string region, int page = 1, int? minPrice = null, int? maxPrice = null, CancellationToken cancellationToken = default);
    Task<List<Listing>> SearchProjectsAsync(string region, int page = 1, int? minPrice = null, int? maxPrice = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches multiple pages and returns aggregated results.
    /// </summary>
    Task<List<Listing>> SearchAllBuyPagesAsync(string region, int maxPages = 5, CancellationToken cancellationToken = default);

    Task<Listing?> GetListingSummaryAsync(int globalId, CancellationToken cancellationToken = default);
    Task<Listing?> GetListingDetailsAsync(string url, CancellationToken cancellationToken = default);
    Task<Listing?> GetContactDetailsAsync(int globalId, CancellationToken cancellationToken = default);
    Task<bool?> CheckFiberAvailabilityAsync(string postalCode, CancellationToken cancellationToken = default);
}
