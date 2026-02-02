using Microsoft.Extensions.Logging;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Services;

public class ListingService : IListingService
{
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

    public async Task ProcessListingsAsync(List<ScrapedListingDto> listings, bool shouldNotify, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing {Count} scraped listings...", listings.Count);

        foreach (var dto in listings)
        {
            try
            {
                await ProcessSingleListingAsync(dto, shouldNotify, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process listing: {FundaId}", dto.FundaId);
            }
        }
    }

    private async Task ProcessSingleListingAsync(ScrapedListingDto dto, bool shouldNotify, CancellationToken cancellationToken)
    {
        var existingListing = await _listingRepository.GetByFundaIdAsync(dto.FundaId, cancellationToken);

        if (existingListing == null)
        {
            await AddNewListingAsync(dto, shouldNotify, cancellationToken);
        }
        else
        {
            await UpdateExistingListingAsync(existingListing, dto, shouldNotify, cancellationToken);
        }
    }

    private async Task AddNewListingAsync(ScrapedListingDto dto, bool shouldNotify, CancellationToken cancellationToken)
    {
        var listing = new Listing
        {
            FundaId = dto.FundaId,
            Address = dto.Address ?? "Unknown Address",
            City = dto.City,
            PostalCode = dto.PostalCode,
            Price = dto.Price,
            Bedrooms = dto.Bedrooms,
            LivingAreaM2 = dto.LivingAreaM2,
            PlotAreaM2 = dto.PlotAreaM2,
            PropertyType = dto.PropertyType,
            Status = dto.Status ?? "Beschikbaar",
            Url = dto.Url,
            ImageUrl = dto.ImageUrl
        };

        await _listingRepository.AddAsync(listing, cancellationToken);
        _logger.LogInformation("Added new listing: {FundaId} - {Address}", listing.FundaId, listing.Address);

        if (shouldNotify)
        {
            await NotifyMatchFoundAsync(listing.Address);
        }

        // Record initial price
        if (listing.Price.HasValue)
        {
            await _priceHistoryRepository.AddAsync(new PriceHistory
            {
                ListingId = listing.Id,
                Price = listing.Price.Value
            }, cancellationToken);
        }
    }

    private async Task UpdateExistingListingAsync(Listing existingListing, ScrapedListingDto dto, bool shouldNotify, CancellationToken cancellationToken)
    {
        // Existing listing - check for price changes
        var priceChanged = existingListing.Price != dto.Price && dto.Price.HasValue;

        if (priceChanged)
        {
            _logger.LogInformation(
                "Price changed for {FundaId}: {OldPrice} -> {NewPrice}",
                dto.FundaId, existingListing.Price, dto.Price);

            await _priceHistoryRepository.AddAsync(new PriceHistory
            {
                ListingId = existingListing.Id,
                Price = dto.Price!.Value
            }, cancellationToken);
        }

        // Update listing properties
        if (dto.Price.HasValue) existingListing.Price = dto.Price;
        if (!string.IsNullOrEmpty(dto.ImageUrl)) existingListing.ImageUrl = dto.ImageUrl;

        // We do NOT overwrite fields that might have been enriched manually or by previous scraper if they are null in the new source
        if (dto.Bedrooms.HasValue) existingListing.Bedrooms = dto.Bedrooms;
        if (dto.LivingAreaM2.HasValue) existingListing.LivingAreaM2 = dto.LivingAreaM2;
        if (dto.PlotAreaM2.HasValue) existingListing.PlotAreaM2 = dto.PlotAreaM2;
        if (!string.IsNullOrEmpty(dto.Status)) existingListing.Status = dto.Status;
        if (!string.IsNullOrEmpty(dto.Address)) existingListing.Address = dto.Address;
        if (!string.IsNullOrEmpty(dto.City)) existingListing.City = dto.City;
        if (!string.IsNullOrEmpty(dto.Url)) existingListing.Url = dto.Url;

        await _listingRepository.UpdateAsync(existingListing, cancellationToken);
        _logger.LogDebug("Updated listing: {FundaId}", dto.FundaId);

        if (shouldNotify)
        {
            await NotifyMatchFoundAsync($"{existingListing.Address} (Updated)");
        }
    }

    private async Task NotifyMatchFoundAsync(string address)
    {
        try
        {
            await _notificationService.NotifyListingFoundAsync(address);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send notification for listing: {Address}", address);
        }
    }
}
