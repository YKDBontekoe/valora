using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.Infrastructure.Scraping;

public interface IFundaMapper
{
    Listing MapApiListingToDomain(FundaApiListing apiListing, string fundaId);
    void EnrichListingWithSummary(Listing listing, FundaApiListingSummary summary);
    void EnrichListingWithNuxtData(Listing listing, FundaNuxtListingData data);
}
