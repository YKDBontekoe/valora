using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class ListingService : IListingService
{
    private const string DefaultStatus = "Beschikbaar";

    private readonly IListingRepository _listingRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly ILogger<ListingService> _logger;

    public ListingService(
        IListingRepository listingRepository,
        IPriceHistoryRepository priceHistoryRepository,
        ILogger<ListingService> logger)
    {
        _listingRepository = listingRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _logger = logger;
    }

    public async Task<ProcessListingResult> ProcessListingAsync(Listing newListing, Listing? existingListing = null, CancellationToken cancellationToken = default)
    {
        if (existingListing == null)
        {
            existingListing = await _listingRepository.GetByFundaIdAsync(newListing.FundaId, cancellationToken);
        }

        if (existingListing == null)
        {
            await AddNewListingAsync(newListing, cancellationToken);
            return new ProcessListingResult(newListing, true, false);
        }
        else
        {
            await UpdateExistingListingAsync(existingListing, newListing, cancellationToken);
            return new ProcessListingResult(existingListing, false, true);
        }
    }

    private async Task AddNewListingAsync(Listing listing, CancellationToken cancellationToken)
    {
        listing.Status ??= DefaultStatus;

        await _listingRepository.AddAsync(listing, cancellationToken);
        _logger.LogInformation("Added new listing: {FundaId} - {Address}", listing.FundaId, listing.Address);

        if (listing.Price.HasValue)
        {
            await _priceHistoryRepository.AddAsync(new PriceHistory
            {
                ListingId = listing.Id,
                Price = listing.Price.Value
            }, cancellationToken);
        }
    }

    private async Task UpdateExistingListingAsync(Listing existingListing, Listing newListing, CancellationToken cancellationToken)
    {
        var priceChanged = existingListing.Price != newListing.Price && newListing.Price.HasValue;

        if (priceChanged)
        {
            _logger.LogInformation(
                "Price changed for {FundaId}: {OldPrice} -> {NewPrice}",
                newListing.FundaId, existingListing.Price, newListing.Price);

            await _priceHistoryRepository.AddAsync(new PriceHistory
            {
                ListingId = existingListing.Id,
                Price = newListing.Price!.Value
            }, cancellationToken);
        }

        existingListing.Price = newListing.Price;
        existingListing.ImageUrl = newListing.ImageUrl;

        existingListing.Merge(newListing);

        await _listingRepository.UpdateAsync(existingListing, cancellationToken);
        _logger.LogDebug("Updated listing: {FundaId}", newListing.FundaId);
    }
}
