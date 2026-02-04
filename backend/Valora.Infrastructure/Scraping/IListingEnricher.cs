using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

public interface IListingEnricher
{
    Task EnrichListingAsync(Listing listing, FundaApiListing apiListing, CancellationToken cancellationToken);
}
