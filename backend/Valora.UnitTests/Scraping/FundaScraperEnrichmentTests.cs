using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Application.Scraping.Interfaces;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Scraping;

public class FundaScraperEnrichmentTests
{
    private readonly Mock<IListingRepository> _listingRepoMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepoMock;
    private readonly Mock<IScraperNotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<FundaScraperService>> _loggerMock;
    private readonly Mock<IFundaApiClient> _apiClientMock;
    private readonly FundaScraperService _service;

    public FundaScraperEnrichmentTests()
    {
        _listingRepoMock = new Mock<IListingRepository>();
        _priceHistoryRepoMock = new Mock<IPriceHistoryRepository>();
        _notificationServiceMock = new Mock<IScraperNotificationService>();
        _loggerMock = new Mock<ILogger<FundaScraperService>>();
        
        _apiClientMock = new Mock<IFundaApiClient>();

        // Default setup for GetByFundaIdsAsync
        _listingRepoMock.Setup(x => x.GetByFundaIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Listing>());

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
    public async Task ProcessListingAsync_ShouldEnrichWithSummary()
    {
        // Arrange
        var apiListing = new Listing { FundaId = "1", Url = "http://url", Address = "Addr1" };
        
        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([apiListing]);

        _apiClientMock.Setup(x => x.GetListingSummaryAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Listing
            { 
                FundaId = "1",
                Address = "Addr1",
                PublicationDate = new DateTime(2023, 1, 1),
                IsSoldOrRented = true,
                Labels = ["Sold"]
            });

        // Act
        await _service.ScrapeLimitedAsync("amsterdam", 1);

        // Assert
        _listingRepoMock.Verify(x => x.AddAsync(It.Is<Listing>(l => 
            l.PublicationDate == new DateTime(2023, 1, 1) &&
            l.IsSoldOrRented == true &&
            l.Labels.Contains("Sold")
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessListingAsync_ShouldEnrichWithContactDetails()
    {
        // Arrange
        var apiListing = new Listing { FundaId = "1", Url = "http://url", Address = "Addr1" };
        
        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([apiListing]);

        _apiClientMock.Setup(x => x.GetContactDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Listing
            { 
                FundaId = "1",
                Address = "Addr1",
                BrokerOfficeId = 100,
                BrokerPhone = "0612345678",
                BrokerLogoUrl = "logo.png",
                AgentName = "Top Makelaar"
            });

        // Act
        await _service.ScrapeLimitedAsync("amsterdam", 1);

        // Assert
        _listingRepoMock.Verify(x => x.AddAsync(It.Is<Listing>(l => 
            l.BrokerOfficeId == 100 &&
            l.BrokerPhone == "0612345678" &&
            l.BrokerLogoUrl == "logo.png" &&
            l.AgentName == "Top Makelaar"
        ), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessListingAsync_ShouldEnrichWithFiberAvailability()
    {
        // Arrange
        var apiListing = new Listing { FundaId = "1", Url = "http://url", Address = "Addr1" };
        
        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([apiListing]);

        // Summary provides postal code
        _apiClientMock.Setup(x => x.GetListingSummaryAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Listing { FundaId = "1", Address = "Addr1", PostalCode = "1234AB" });

        _apiClientMock.Setup(x => x.CheckFiberAvailabilityAsync("1234AB", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _service.ScrapeLimitedAsync("amsterdam", 1);

        // Assert
        _listingRepoMock.Verify(x => x.AddAsync(It.Is<Listing>(l => 
            l.FiberAvailable == true
        ), It.IsAny<CancellationToken>()), Times.Once);
    }
}
