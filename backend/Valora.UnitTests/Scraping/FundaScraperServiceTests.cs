using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Scraping;
using Valora.Infrastructure.Scraping;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.UnitTests.Scraping;

public class FundaScraperServiceTests
{
    private readonly Mock<IListingService> _listingServiceMock;
    private readonly Mock<IScraperNotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<FundaScraperService>> _loggerMock;
    private readonly Mock<FundaApiClient> _apiClientMock;
    private readonly FundaScraperService _service;

    public FundaScraperServiceTests()
    {
        _listingServiceMock = new Mock<IListingService>();
        _notificationServiceMock = new Mock<IScraperNotificationService>();
        _loggerMock = new Mock<ILogger<FundaScraperService>>();
        
        // Mock FundaApiClient using a loose mock (mocking virtual methods)
        _apiClientMock = new Mock<FundaApiClient>(new HttpClient(), Mock.Of<ILogger<FundaApiClient>>());

        var options = Options.Create(new ScraperOptions
        {
            SearchUrls = ["https://www.funda.nl/koop/amsterdam/"],
            DelayBetweenRequestsMs = 0 
        });

        _service = new FundaScraperService(
            _apiClientMock.Object,
            _listingServiceMock.Object,
            options,
            _loggerMock.Object,
            _notificationServiceMock.Object
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

        // Verify completion
        _notificationServiceMock.Verify(x => x.NotifyCompleteAsync(), Times.Once);

        // Verify ListingService call
        _listingServiceMock.Verify(x => x.ProcessListingsAsync(
            It.Is<List<ScrapedListingDto>>(l => l.Count == 2 && l[0].FundaId == "123" && l[1].FundaId == "456"),
            true,
            It.IsAny<CancellationToken>()), Times.Once);
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
            _apiClientMock.Object,
            _listingServiceMock.Object,
            options,
            _loggerMock.Object,
            _notificationServiceMock.Object
        );

        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync("city1", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { GlobalId = 1, ListingUrl = "u1", Address = new() { City = "City1" } }]);
        
        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync("city2", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new() { GlobalId = 2, ListingUrl = "u2", Address = new() { City = "City2" } }]);

        // Act
        await service.ScrapeAndStoreAsync();

        // Assert
        // Should verify ProcessListingsAsync called 2 times (once per URL)
        _listingServiceMock.Verify(x => x.ProcessListingsAsync(It.IsAny<List<ScrapedListingDto>>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
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
        // Verify ListingService call receives only 1 item
        _listingServiceMock.Verify(x => x.ProcessListingsAsync(
            It.Is<List<ScrapedListingDto>>(l => l.Count == 1),
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
