using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Domain.Entities;
using Valora.Infrastructure.Scraping;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.UnitTests.Scraping;

public class FundaSearchServiceTests
{
    private readonly Mock<IFundaApiClient> _apiClientMock;
    private readonly Mock<IListingRepository> _listingRepoMock;
    private readonly Mock<IListingService> _listingServiceMock;
    private readonly Mock<ILogger<FundaSearchService>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly FundaSearchService _service;

    public FundaSearchServiceTests()
    {
        _apiClientMock = new Mock<IFundaApiClient>();
        _listingRepoMock = new Mock<IListingRepository>();
        _listingServiceMock = new Mock<IListingService>();
        _loggerMock = new Mock<ILogger<FundaSearchService>>();
        _configMock = new Mock<IConfiguration>();

        // Setup configuration defaults
        var configSectionMock = new Mock<IConfigurationSection>();
        configSectionMock.Setup(x => x.Value).Returns("60");
        _configMock.Setup(x => x.GetSection("CACHE_FRESHNESS_MINUTES")).Returns(configSectionMock.Object);
        _configMock.Setup(x => x.GetSection("SEARCH_CACHE_MINUTES")).Returns(configSectionMock.Object);

        _service = new FundaSearchService(
            _apiClientMock.Object,
            _listingRepoMock.Object,
            _listingServiceMock.Object,
            _configMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task SearchAsync_ShouldFetchFromApi_WhenCacheIsStale()
    {
        // Arrange
        var query = new FundaSearchQuery(Region: "amsterdam", OfferingType: "buy");
        
        _apiClientMock.Setup(x => x.SearchBuyAsync("amsterdam", It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundaApiResponse 
            { 
                Listings = new List<FundaApiListing> 
                { 
                    new() { GlobalId = 1, ListingUrl = "http://url", Address = new() { ListingAddress = "Addr1" } } 
                } 
            });

        _listingRepoMock.Setup(x => x.GetByCityAsync("amsterdam", It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Listing> { new() { FundaId = "1", Address = "Addr1" } });

        // Act
        var result = await _service.SearchAsync(query);

        // Assert
        Assert.Single(result.Items);
        Assert.False(result.FromCache);
        _apiClientMock.Verify(x => x.SearchBuyAsync("amsterdam", It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);

        // Verify ListingService CreateListingAsync called
        _listingServiceMock.Verify(x => x.CreateListingAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ShouldUseCache_WhenFresh()
    {
        // Arrange
        var query = new FundaSearchQuery(Region: "amsterdam", OfferingType: "buy");
        
        // First call to populate cache
        _apiClientMock.Setup(x => x.SearchBuyAsync("amsterdam", It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundaApiResponse { Listings = new List<FundaApiListing>() });
        
        _listingRepoMock.Setup(x => x.GetByCityAsync("amsterdam", It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Listing>());

        await _service.SearchAsync(query);

        // Reset verify
        _apiClientMock.Invocations.Clear();

        // Act - Second call
        await _service.SearchAsync(query);

        // Assert
        // Should NOT call API again
        _apiClientMock.Verify(x => x.SearchBuyAsync("amsterdam", It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByFundaUrlAsync_ShouldReturnFreshCachedListing()
    {
        // Arrange
        var url = "https://www.funda.nl/koop/amsterdam/appartement-42424242-test/42424242/";
        var listing = new Listing { FundaId = "42424242", LastFundaFetchUtc = DateTime.UtcNow, Address = "Test Address" };

        _listingRepoMock.Setup(x => x.GetByFundaIdAsync("42424242", It.IsAny<CancellationToken>()))
            .ReturnsAsync(listing);

        // Act
        var result = await _service.GetByFundaUrlAsync(url);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("42424242", result.FundaId);
        _apiClientMock.Verify(x => x.GetListingSummaryAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetByFundaUrlAsync_ShouldFetchFromApi_WhenCacheMiss()
    {
        // Arrange
        var url = "https://www.funda.nl/koop/amsterdam/appartement-42424242-test/42424242/";
        
        _listingRepoMock.Setup(x => x.GetByFundaIdAsync("42424242", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing?)null);

        _apiClientMock.Setup(x => x.GetListingSummaryAsync(42424242, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FundaApiListingSummary 
            { 
                Address = new() { Street = "Teststraat", City = "Amsterdam" },
                Identifiers = new() { GlobalId = 42424242 }
            });

        // Act
        var result = await _service.GetByFundaUrlAsync(url);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("42424242", result.FundaId);
        _apiClientMock.Verify(x => x.GetListingSummaryAsync(42424242, It.IsAny<CancellationToken>()), Times.Once);

        // Verify ListingService CreateListingAsync
        _listingServiceMock.Verify(x => x.CreateListingAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByFundaUrlAsync_ShouldParseIdCorrectly()
    {
         var url = "https://www.funda.nl/detail/koop/amsterdam/appartement-met-id/12345678/";
         
        _listingRepoMock.Setup(x => x.GetByFundaIdAsync("12345678", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Listing { FundaId = "12345678", LastFundaFetchUtc = DateTime.UtcNow, Address = "Test Address" });

         var result = await _service.GetByFundaUrlAsync(url);
         
         Assert.NotNull(result);
         Assert.Equal("12345678", result.FundaId);
    }
}
