using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Application.Services;
using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.UnitTests.Scraping;

public class FundaScraperServiceTests
{
    private readonly Mock<IListingRepository> _listingRepoMock;
    private readonly Mock<IListingService> _listingServiceMock;
    private readonly Mock<IScraperNotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<FundaScraperService>> _loggerMock;
    private readonly Mock<IFundaApiClient> _apiClientMock;
    private readonly FundaScraperService _service;

    public FundaScraperServiceTests()
    {
        _listingRepoMock = new Mock<IListingRepository>();
        _listingServiceMock = new Mock<IListingService>();
        _notificationServiceMock = new Mock<IScraperNotificationService>();
        _loggerMock = new Mock<ILogger<FundaScraperService>>();
        _apiClientMock = new Mock<IFundaApiClient>();

        // Default setup for GetByFundaIdsAsync to return empty list
        _listingRepoMock.Setup(x => x.GetByFundaIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Listing>());

        // Default setup for ProcessListingAsync to return "New" result
        _listingServiceMock.Setup(x => x.ProcessListingAsync(It.IsAny<Listing>(), It.IsAny<Listing?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing l, Listing? e, CancellationToken t) => new ProcessListingResult(l, true, false));

        var options = Options.Create(new ScraperOptions
        {
            SearchUrls = [],
            DelayBetweenRequestsMs = 0 
        });

        _service = new FundaScraperService(
            _listingRepoMock.Object,
            _listingServiceMock.Object,
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

        // Verify listing service calls
        _listingServiceMock.Verify(x => x.ProcessListingAsync(It.IsAny<Listing>(), null, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ScrapeAndStoreAsync_ShouldProcessAllUrls()
    {
        // Arrange
        var options = Options.Create(new ScraperOptions
        {
            SearchUrls = ["https://www.funda.nl/koop/city1/", "https://www.funda.nl/koop/city2/"],
            DelayBetweenRequestsMs = 0
        });

        var service = new FundaScraperService(
            _listingRepoMock.Object,
            _listingServiceMock.Object,
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
        // Should verify ProcessListingAsync called 2 times
        _listingServiceMock.Verify(x => x.ProcessListingAsync(It.IsAny<Listing>(), null, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ScrapeAndStoreAsync_ShouldPassExistingListingToService()
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
            Price = 500000,
            Address = "Addr1"
        };

        _listingRepoMock.Setup(x => x.GetByFundaIdsAsync(It.Is<IEnumerable<string>>(ids => ids.Contains("1")), It.IsAny<CancellationToken>()))
            .ReturnsAsync([existingListing]);

        // Setup service to return Updated result
        _listingServiceMock.Setup(x => x.ProcessListingAsync(It.IsAny<Listing>(), existingListing, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessListingResult(existingListing, false, true));

        var options = Options.Create(new ScraperOptions
        {
            SearchUrls = ["https://www.funda.nl/koop/amsterdam/"],
            DelayBetweenRequestsMs = 0
        });

        var service = new FundaScraperService(
            _listingRepoMock.Object,
            _listingServiceMock.Object,
            options,
            _loggerMock.Object,
            _notificationServiceMock.Object,
            _apiClientMock.Object
        );

        // Act
        await service.ScrapeAndStoreAsync();

        // Assert
        // Should call ProcessListingAsync with existing listing
        _listingServiceMock.Verify(x => x.ProcessListingAsync(
            It.Is<Listing>(l => l.FundaId == "1"),
            existingListing,
            It.IsAny<CancellationToken>()), Times.Once);

        // Should verify notification for updated listing
        _notificationServiceMock.Verify(x => x.NotifyListingFoundAsync(It.Is<string>(s => s.Contains("(Updated)"))), Times.Never);
    }

    [Fact]
    public async Task ScrapeLimitedAsync_ShouldEnforceLimit()
    {
        // Arrange
        var apiListings = new List<FundaApiListing>
        {
            new() { GlobalId = 1, ListingUrl = "u1", Address = new() { ListingAddress = "1" } },
            new() { GlobalId = 2, ListingUrl = "u2", Address = new() { ListingAddress = "2" } },
            new() { GlobalId = 3, ListingUrl = "u3", Address = new() { ListingAddress = "3" } }
        };

        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync("amsterdam", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiListings);

        // Act
        await _service.ScrapeLimitedAsync("amsterdam", 1);

        // Assert
        _listingServiceMock.Verify(x => x.ProcessListingAsync(It.IsAny<Listing>(), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ScrapeLimitedAsync_ShouldHandleApiErrors_AndNotify()
    {
        // Arrange
        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync("amsterdam", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Error"));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.ScrapeLimitedAsync("amsterdam", 10));

        // Verify error notification
        _notificationServiceMock.Verify(x => x.NotifyErrorAsync(It.Is<string>(s => s.Contains("API Error"))), Times.Once);
    }

    [Fact]
    public async Task EnrichListingAsync_EnrichmentFailures_AreLoggedButDontStopProcess()
    {
        // Arrange
        var apiListing = new FundaApiListing { GlobalId = 1, ListingUrl = "url", Address = new() { ListingAddress = "Addr" } };
        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync("amsterdam", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([apiListing]);

        // Mock all enrichment steps to throw
        _apiClientMock.Setup(x => x.GetListingSummaryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Summary Failed"));

        _apiClientMock.Setup(x => x.GetListingDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Nuxt Failed"));

        _apiClientMock.Setup(x => x.GetContactDetailsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Contacts Failed"));

        // Act
        await _service.ScrapeLimitedAsync("amsterdam", 1);

        // Assert
        // Should still process the listing
        _listingServiceMock.Verify(x => x.ProcessListingAsync(It.IsAny<Listing>(), null, It.IsAny<CancellationToken>()), Times.Once);

        // Should verify logger warnings
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeast(2));

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }
}
