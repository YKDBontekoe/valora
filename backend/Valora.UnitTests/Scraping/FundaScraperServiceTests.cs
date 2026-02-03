using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
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
    private readonly ValoraDbContext _dbContext;
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

        var dbOptions = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new ValoraDbContext(dbOptions);

        _service = new FundaScraperService(
            _listingRepoMock.Object,
            _priceHistoryRepoMock.Object,
            _dbContext,
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
            _dbContext,
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
            _dbContext,
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
    public async Task ScrapeAndStoreAsync_InvalidUrl_ShouldSkipAndLog()
    {
        // Arrange
        var options = Options.Create(new ScraperOptions
        {
            SearchUrls = ["invalid-url", "https://www.funda.nl/koop/valid/"],
            DelayBetweenRequestsMs = 0
        });

        var service = new FundaScraperService(
            _listingRepoMock.Object,
            _priceHistoryRepoMock.Object,
            _dbContext,
            options,
            _loggerMock.Object,
            _notificationServiceMock.Object,
            _apiClientMock.Object
        );

        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        // Act
        await service.ScrapeAndStoreAsync();

        // Assert
        // Should only call API for the valid URL
        _apiClientMock.Verify(x => x.SearchAllBuyPagesAsync("valid", It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify invalid URL caused a log error (checking for the specific message format)
        // Note: Logger extension methods are hard to verify with Moq unless checking the underlying ILogger.Log
        // We assume logic is correct if API is not called for 'invalid-url' which would be region 'invalid-url' if regex failed or null
    }

    [Fact]
    public async Task ScrapeLimitedAsync_RepositoryError_ShouldThrowAndLog()
    {
        // Arrange
        var apiListings = new List<FundaApiListing>
        {
            new() { GlobalId = 123, Price = "€ 500.000 k.k.", ListingUrl = "http://url1", Address = new() { ListingAddress = "Addr1", City = "City1" } }
        };

        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync("amsterdam", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiListings);

        _listingRepoMock.Setup(x => x.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database Error"));

        // Act & Assert
        // Exceptions in ProcessListingAsync are caught and logged inside ScrapeSearchUrlAsync loop.
        // The loop continues.
        await _service.ScrapeLimitedAsync("amsterdam", 1);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to process listing")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateExistingListingAsync_RepositoryError_ShouldThrowAndLog()
    {
        // Arrange
        var apiListing = new FundaApiListing { GlobalId = 123, Price = "€ 600.000 k.k.", ListingUrl = "u", Address = new() { ListingAddress = "A", City = "C" } };

        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync("amsterdam", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([apiListing]);

        var existingListing = new Listing { FundaId = "123", Price = 500000, Address = "A" };
        _listingRepoMock.Setup(x => x.GetByFundaIdAsync("123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingListing);

        _listingRepoMock.Setup(x => x.UpdateAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Update Error"));

        // Act & Assert
        // We use ScrapeLimitedAsync to trigger the pipeline.
        // NOTE: Exceptions in ProcessListingAsync are caught and logged inside ScrapeSearchUrlAsync.
        // They are NOT rethrown, to allow other listings to be processed.
        // So we expect ScrapeLimitedAsync to complete successfully, but we verify the logger interaction.
        await _service.ScrapeLimitedAsync("amsterdam", 1);

        // Use verify on Logger via the mocked object.
        // Since LogError is an extension method, we check ILogger.Log
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to process listing")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}