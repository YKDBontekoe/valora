using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

public interface IFundaApiClient
{
    Task<FundaApiListingSummary?> GetListingSummaryAsync(int globalId, CancellationToken cancellationToken = default);
    Task<FundaContactDetailsResponse?> GetContactDetailsAsync(int globalId, CancellationToken cancellationToken = default);
    Task<FundaFiberResponse?> GetFiberAvailabilityAsync(string postalCode, CancellationToken cancellationToken = default);
    Task<FundaApiResponse?> SearchBuyAsync(string geoInfo, int page = 1, int? minPrice = null, int? maxPrice = null, CancellationToken cancellationToken = default);
    Task<FundaApiResponse?> SearchRentAsync(string geoInfo, int page = 1, int? minPrice = null, int? maxPrice = null, CancellationToken cancellationToken = default);
    Task<FundaApiResponse?> SearchProjectsAsync(string geoInfo, int page = 1, int? minPrice = null, int? maxPrice = null, CancellationToken cancellationToken = default);
    Task<List<FundaApiListing>> SearchAllBuyPagesAsync(string geoInfo, int maxPages = 5, CancellationToken cancellationToken = default);
    Task<FundaNuxtListingData?> GetListingDetailsAsync(string url, CancellationToken cancellationToken = default);
}
