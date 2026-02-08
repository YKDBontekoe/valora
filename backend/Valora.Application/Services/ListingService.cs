using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class ListingService : IListingService
{
    private const string DefaultStatus = "Beschikbaar";
    private readonly IListingRepository _listingRepository;
    private readonly IPriceHistoryRepository _priceHistoryRepository;
    private readonly IScraperNotificationService _notificationService;
    private readonly ILogger<ListingService> _logger;

    public ListingService(
        IListingRepository listingRepository,
        IPriceHistoryRepository priceHistoryRepository,
        IScraperNotificationService notificationService,
        ILogger<ListingService> logger)
    {
        _listingRepository = listingRepository;
        _priceHistoryRepository = priceHistoryRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<List<Listing>> GetListingsByFundaIdsAsync(List<string> fundaIds, CancellationToken cancellationToken)
    {
        return await _listingRepository.GetByFundaIdsAsync(fundaIds, cancellationToken);
    }

    public async Task SaveListingAsync(Listing newListing, Listing? existingListing, bool shouldNotify, CancellationToken cancellationToken)
    {
        if (existingListing == null)
        {
            await AddNewListingAsync(newListing, shouldNotify, cancellationToken);
        }
        else
        {
            await UpdateExistingListingAsync(existingListing, newListing, shouldNotify, cancellationToken);
        }
    }

    private async Task AddNewListingAsync(Listing listing, bool shouldNotify, CancellationToken cancellationToken)
    {
        listing.Status ??= DefaultStatus;
        listing.LastFundaFetchUtc = DateTime.UtcNow;

        await _listingRepository.AddAsync(listing, cancellationToken);
        _logger.LogInformation("Added new listing: {FundaId}", listing.FundaId);

        if (shouldNotify)
        {
            await NotifyMatchFoundAsync(listing.FundaId);
        }

        if (listing.Price.HasValue)
        {
            await _priceHistoryRepository.AddAsync(new PriceHistory
            {
                ListingId = listing.Id,
                Price = listing.Price.Value
            }, cancellationToken);
        }
    }

    private async Task UpdateExistingListingAsync(Listing existingListing, Listing listing, bool shouldNotify, CancellationToken cancellationToken)
    {
        var priceChanged = existingListing.Price != listing.Price && listing.Price.HasValue;

        if (priceChanged)
        {
            _logger.LogInformation(
                "Price changed for {FundaId}: {OldPrice} -> {NewPrice}",
                listing.FundaId, existingListing.Price, listing.Price);

            await _priceHistoryRepository.AddAsync(new PriceHistory
            {
                ListingId = existingListing.Id,
                Price = listing.Price!.Value
            }, cancellationToken);
        }

        // Use the Merge method on the entity
        existingListing.Merge(listing);

        await _listingRepository.UpdateAsync(existingListing, cancellationToken);
        _logger.LogDebug("Updated listing: {FundaId}", listing.FundaId);

        if (shouldNotify)
        {
            await NotifyMatchFoundAsync(listing.FundaId);
        }
    }

    private async Task NotifyMatchFoundAsync(string fundaId)
    {
        try
        {
            await _notificationService.NotifyListingFoundAsync(fundaId);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send notification for listing: {FundaId}", fundaId);
        }
    }
}
