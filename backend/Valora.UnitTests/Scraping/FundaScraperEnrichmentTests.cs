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

public class FundaScraperEnrichmentTests
{
    private readonly Mock<IListingRepository> _listingRepoMock;
    private readonly Mock<IListingService> _listingServiceMock;
    private readonly Mock<IScraperNotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<FundaScraperService>> _loggerMock;
    private readonly Mock<IFundaApiClient> _apiClientMock;
    private readonly FundaScraperService _service;

    public FundaScraperEnrichmentTests()
    {
        _listingRepoMock = new Mock<IListingRepository>();
        _listingServiceMock = new Mock<IListingService>();
        _notificationServiceMock = new Mock<IScraperNotificationService>();
        _loggerMock = new Mock<ILogger<FundaScraperService>>();
        _apiClientMock = new Mock<IFundaApiClient>();

        // Default setup for GetByFundaIdsAsync
        _listingRepoMock.Setup(x => x.GetByFundaIdsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Listing>());

        // Default setup for ProcessListingAsync
        _listingServiceMock.Setup(x => x.ProcessListingAsync(It.IsAny<Listing>(), It.IsAny<Listing?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcessListingResult(new Listing { FundaId = "1", Address = "A" }, true, false));

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
    public async Task ProcessListingAsync_ShouldEnrichWithSummary()
    {
        // Arrange
        var apiListing = new FundaApiListing { GlobalId = 1, ListingUrl = "http://url", Address = new() { ListingAddress = "Addr1" } };
        
        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([apiListing]);

        _apiClientMock.Setup(x => x.GetListingSummaryAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundaApiListingSummary 
            { 
                PublicationDate = new DateTime(2023, 1, 1),
                IsSoldOrRented = true,
                Labels = [new() { Text = "Sold" }]
            });

        // Act
        await _service.ScrapeLimitedAsync("amsterdam", 1);

        // Assert
        _listingServiceMock.Verify(x => x.ProcessListingAsync(It.Is<Listing>(l =>
            l.PublicationDate == new DateTime(2023, 1, 1) &&
            l.IsSoldOrRented == true &&
            l.Labels.Contains("Sold")
        ), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessListingAsync_ShouldEnrichWithContactDetails()
    {
        // Arrange
        var apiListing = new FundaApiListing { GlobalId = 1, ListingUrl = "http://url", Address = new() { ListingAddress = "Addr1" } };
        
        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([apiListing]);

        _apiClientMock.Setup(x => x.GetContactDetailsAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundaContactDetailsResponse 
            { 
                ContactDetails = [
                    new() { 
                        Id = 100, 
                        PhoneNumber = "0612345678", 
                        LogoUrl = "logo.png",
                        DisplayName = "Top Makelaar"
                    }
                ]
            });

        // Act
        await _service.ScrapeLimitedAsync("amsterdam", 1);

        // Assert
        _listingServiceMock.Verify(x => x.ProcessListingAsync(It.Is<Listing>(l =>
            l.BrokerOfficeId == 100 &&
            l.BrokerPhone == "0612345678" &&
            l.BrokerLogoUrl == "logo.png" &&
            l.AgentName == "Top Makelaar"
        ), null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessListingAsync_ShouldEnrichWithFiberAvailability()
    {
        // Arrange
        var apiListing = new FundaApiListing { GlobalId = 1, ListingUrl = "http://url", Address = new() { ListingAddress = "Addr1" } };
        
        _apiClientMock.Setup(x => x.SearchAllBuyPagesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([apiListing]);

        // Summary provides postal code
        _apiClientMock.Setup(x => x.GetListingSummaryAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundaApiListingSummary { Address = new() { PostalCode = "1234AB" } });

        _apiClientMock.Setup(x => x.GetFiberAvailabilityAsync("1234AB", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundaFiberResponse { Availability = true });

        // Act
        await _service.ScrapeLimitedAsync("amsterdam", 1);

        // Assert
        _listingServiceMock.Verify(x => x.ProcessListingAsync(It.Is<Listing>(l =>
            l.FiberAvailable == true
        ), null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
