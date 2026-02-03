using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.UnitTests.Scraping;

public class FundaScraperServiceTests
{
    private readonly Mock<IListingRepository> _listingRepoMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepoMock;
    private readonly Mock<IScraperNotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<FundaScraperService>> _loggerMock;
    private readonly Mock<FundaApiClient> _apiClientMock;
    private readonly FundaScraperService _service;

    public FundaScraperServiceTests()
    {
        _listingRepoMock = new Mock<IListingRepository>();
        _priceHistoryRepoMock = new Mock<IPriceHistoryRepository>();
        _notificationServiceMock = new Mock<IScraperNotificationService>();
        _loggerMock = new Mock<ILogger<FundaScraperService>>();
        
        // Mock FundaApiClient using a loose mock (mocking virtual methods)
        // We pass dummy dependencies to the base constructor because it's a class mock
        _apiClientMock = new Mock<FundaApiClient>(new HttpClient(), Mock.Of<ILogger<FundaApiClient>>());

        var options = Options.Create(new ScraperOptions
        {
            SearchUrls = [],
            DelayBetweenRequestsMs = 0 
        });

        _service = new FundaScraperService(
            _listingRepoMock.Object,
            _priceHistoryRepoMock.Object,
            options,
            _loggerMock.Object,
            _notificationServiceMock.Object,
            _apiClientMock.Object
        );
    }

    [Fact]
    public async Task ScrapeLimitedAsync_ShouldProcessListings_AndSendNotifications()
    {
        // Arrange
        var apiListings = new List<FundaApiListing>
        {
            new() { GlobalId = 123, Price = "€ 500.000 k.k.", ListingUrl = "http://url1", Address = new() { ListingAddress = "Addr1", City = "City1" } },
            new() { GlobalId = 456, Price = "€ 600.000 k.k.", ListingUrl = "http://url2", Address = new() { ListingAddress = "Addr2", City = "City2" } }
        };

        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync("amsterdam", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiListings);

        // Act
        await _service.ScrapeLimitedAsync("amsterdam", 2);

        // Assert
        // Verify notifications
        _notificationServiceMock.Verify(x => x.NotifyProgressAsync(It.Is<string>(s => s.Contains("Starting search"))), Times.Once);
        _notificationServiceMock.Verify(x => x.NotifyProgressAsync(It.Is<string>(s => s.Contains("Fetching search results"))), Times.Once);
        _notificationServiceMock.Verify(x => x.NotifyProgressAsync(It.Is<string>(s => s.Contains("Found 2 listings"))), Times.Once);

        // Verify 2 found notifications
        _notificationServiceMock.Verify(x => x.NotifyListingFoundAsync(It.IsAny<string>()), Times.Exactly(2));

        // Verify completion
        _notificationServiceMock.Verify(x => x.NotifyCompleteAsync(), Times.Once);

        // Verify repository calls
        _listingRepoMock.Verify(x => x.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ScrapeAndStoreAsync_ShouldProcessAllUrls()
    {
        // Arrange
        // Re-create service with multiple URLs
        var options = Options.Create(new ScraperOptions
        {
            SearchUrls = ["https://www.funda.nl/koop/city1/", "https://www.funda.nl/koop/city2/"],
            DelayBetweenRequestsMs = 0
        });

        var service = new FundaScraperService(
            _listingRepoMock.Object,
            _priceHistoryRepoMock.Object,
            options,
            _loggerMock.Object,
            _notificationServiceMock.Object,
            _apiClientMock.Object
        );

        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync("city1", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { GlobalId = 1, ListingUrl = "u1", Address = new() { City = "City1" } }]);
        
        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync("city2", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { GlobalId = 2, ListingUrl = "u2", Address = new() { City = "City2" } }]);

        // Act
        await service.ScrapeAndStoreAsync();

        // Assert
        // Should verify AddAsync called 2 times (once per URL finding 1 listing)
        _listingRepoMock.Verify(x => x.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ScrapeAndStoreAsync_ShouldUpdateExistingListings()
    {
        // Arrange
        var apiListing = new FundaApiListing 
        { 
            GlobalId = 1, 
            Price = "€ 600.000 k.k.", 
            ListingUrl = "url", 
            Address = new() { ListingAddress = "Addr1" } 
        };

        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync("amsterdam", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([apiListing]);

        var existingListing = new Listing
        {
            Id = Guid.NewGuid(),
            FundaId = "1",
            Price = 500000, // Old price
            Address = "Addr1"
        };

        _listingRepoMock.Setup(x => x.GetByFundaIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingListing);

        var options = Options.Create(new ScraperOptions
        {
            SearchUrls = ["https://www.funda.nl/koop/amsterdam/"],
            DelayBetweenRequestsMs = 0
        });

        var service = new FundaScraperService(
            _listingRepoMock.Object,
            _priceHistoryRepoMock.Object,
            options,
            _loggerMock.Object,
            _notificationServiceMock.Object,
            _apiClientMock.Object
        );

        // Act
        await service.ScrapeAndStoreAsync();

        // Assert
        // Should update listing
        _listingRepoMock.Verify(x => x.UpdateAsync(It.Is<Listing>(l => l.Price == 600000), It.IsAny<CancellationToken>()), Times.Once);

        // Should add price history
        _priceHistoryRepoMock.Verify(x => x.AddAsync(It.Is<PriceHistory>(ph => ph.Price == 600000 && ph.ListingId == existingListing.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScrapeLimitedAsync_ShouldEnforceLimit()
    {
        // Arrange
        // API returns 3 listings
        var apiListings = new List<FundaApiListing>
        {
            new() { GlobalId = 1, ListingUrl = "u1", Address = new() { ListingAddress = "1" } },
            new() { GlobalId = 2, ListingUrl = "u2", Address = new() { ListingAddress = "2" } },
            new() { GlobalId = 3, ListingUrl = "u3", Address = new() { ListingAddress = "3" } }
        };

        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync("amsterdam", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiListings);

        // Act
        // Limit to 1
        await _service.ScrapeLimitedAsync("amsterdam", 1);

        // Assert
        // Note: The service logic applies limit inside ScrapeLimitedAsync calling ScrapeSearchUrlAsync with limit.
        // And ScrapeSearchUrlAsync -> TryFetchFromApiAsync applies the Take(limit).
        
        // Should only add 1 listing
        _listingRepoMock.Verify(x => x.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScrapeLimitedAsync_ShouldHandleApiErrors_AndNotify()
    {
        // Arrange
        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync("amsterdam", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act & Assert
        // The service logic now lets exceptions bubble up so the job can fail (and retry via Hangfire).
        // It should still notify the user about the error.
        
        await Assert.ThrowsAsync<Exception>(() => _service.ScrapeLimitedAsync("amsterdam", 10));

        // Verify error notification with the actual exception message
        _notificationServiceMock.Verify(x => x.NotifyErrorAsync(It.Is<string>(s => s.Contains("API Error"))), Times.Once);
    }

    [Fact]
    public async Task UpdateExistingListingsAsync_ShouldUpdatePrice_WhenChanged()
    {
        // Arrange
        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            FundaId = "123",
            Price = 500000,
            Address = "Test Address",
            Status = "Beschikbaar"
        };

        _listingRepoMock.Setup(x => x.GetActiveListingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([listing]);

        // Mock GetListingSummaryAsync
        _apiClientMock.Setup(x => x.GetListingSummaryAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundaApiListingSummary
            {
                Identifiers = new FundaApiIdentifiers { GlobalId = 123 },
                Price = new FundaApiSummaryPrice { SellingPrice = "€ 550.000 k.k." }
            });

        // Act
        await _service.UpdateExistingListingsAsync();

        // Assert
        // Verify price update on object
        Assert.Equal(550000, listing.Price);

        // Verify update call
        _listingRepoMock.Verify(x => x.UpdateAsync(listing, It.IsAny<CancellationToken>()), Times.Once);

        // Verify history added
        _priceHistoryRepoMock.Verify(x => x.AddAsync(It.Is<PriceHistory>(ph => ph.Price == 550000), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateExistingListingsAsync_ShouldSkip_WhenFundaIdInvalid()
    {
        // Arrange
        var listing = new Listing { FundaId = "invalid-id", Address = "A", Status = "Active" };
        _listingRepoMock.Setup(x => x.GetActiveListingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([listing]);

        // Act
        await _service.UpdateExistingListingsAsync();

        // Assert
        // Should not call API
        _apiClientMock.Verify(x => x.GetListingSummaryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateExistingListingsAsync_ShouldSkip_WhenSummaryIsNull()
    {
        // Arrange
        var listing = new Listing { FundaId = "123", Address = "A", Status = "Active" };
        _listingRepoMock.Setup(x => x.GetActiveListingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([listing]);

        _apiClientMock.Setup(x => x.GetListingSummaryAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((FundaApiListingSummary?)null);

        // Act
        await _service.UpdateExistingListingsAsync();

        // Assert
        _listingRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateExistingListingsAsync_ShouldNotUpdate_WhenPriceUnchanged()
    {
        // Arrange
        var listing = new Listing { FundaId = "123", Price = 500000, Address = "A", Status = "Active" };
        _listingRepoMock.Setup(x => x.GetActiveListingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([listing]);

        _apiClientMock.Setup(x => x.GetListingSummaryAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundaApiListingSummary
            {
                Identifiers = new FundaApiIdentifiers { GlobalId = 123 },
                Price = new FundaApiSummaryPrice { SellingPrice = "€ 500.000 k.k." }
            });

        // Act
        await _service.UpdateExistingListingsAsync();

        // Assert
        _listingRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Never);
        _priceHistoryRepoMock.Verify(x => x.AddAsync(It.IsAny<PriceHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateExistingListingsAsync_ShouldHandleApiExceptions()
    {
        // Arrange
        var listing1 = new Listing { FundaId = "123", Address = "A" };
        var listing2 = new Listing { FundaId = "456", Address = "B" };

        _listingRepoMock.Setup(x => x.GetActiveListingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([listing1, listing2]);

        // Fail first one
        _apiClientMock.Setup(x => x.GetListingSummaryAsync(123, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Not Found"));

        // Succeed second one
        _apiClientMock.Setup(x => x.GetListingSummaryAsync(456, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundaApiListingSummary
            {
                Identifiers = new FundaApiIdentifiers { GlobalId = 456 },
                Price = new FundaApiSummaryPrice { SellingPrice = "€ 400.000 k.k." }
            });

        // Act
        await _service.UpdateExistingListingsAsync();

        // Assert
        // First one failed but loop continued
        _apiClientMock.Verify(x => x.GetListingSummaryAsync(456, It.IsAny<CancellationToken>()), Times.Once);
    }
}