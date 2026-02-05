using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Scraping;
using Valora.Application.Scraping.Interfaces;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Scraping;

public class FundaSearchServiceTests
{
    private readonly Mock<IFundaApiClient> _apiClientMock;
    private readonly Mock<IListingRepository> _listingRepoMock;
    private readonly Mock<ILogger<FundaSearchService>> _loggerMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly IMemoryCache _cache;
    private readonly FundaSearchService _service;

    public FundaSearchServiceTests()
    {
        _apiClientMock = new Mock<IFundaApiClient>();
        _listingRepoMock = new Mock<IListingRepository>();
        _loggerMock = new Mock<ILogger<FundaSearchService>>();
        _configMock = new Mock<IConfiguration>();
        _cache = new MemoryCache(new MemoryCacheOptions());

        // Setup configuration defaults
        var configSectionMock = new Mock<IConfigurationSection>();
        configSectionMock.Setup(x => x.Value).Returns("60");
        _configMock.Setup(x => x.GetSection("CACHE_FRESHNESS_MINUTES")).Returns(configSectionMock.Object);
        _configMock.Setup(x => x.GetSection("SEARCH_CACHE_MINUTES")).Returns(configSectionMock.Object);

        // Default setups to prevent NullReferenceException on await null Task
        _apiClientMock
            .Setup(x => x.SearchBuyAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Listing>());
        _apiClientMock
            .Setup(x => x.SearchRentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Listing>());
        _apiClientMock
            .Setup(x => x.SearchProjectsAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Listing>());
        _apiClientMock
            .Setup(x => x.GetListingDetailsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing?)null);

        _service = new FundaSearchService(
            _apiClientMock.Object,
            _listingRepoMock.Object,
            _cache,
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
            .ReturnsAsync(new List<Listing>
            { 
                new() { FundaId = "1", Url = "http://url", Address = "Addr1", City = "Amsterdam" }
            });

        _listingRepoMock.Setup(x => x.GetByCityAsync("amsterdam", It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Listing> { new() { FundaId = "1", Address = "Addr1" } });

        // Act
        var result = await _service.SearchAsync(query);

        // Assert
        Assert.Single(result.Items);
        Assert.False(result.FromCache);
        _apiClientMock.Verify(x => x.SearchBuyAsync("amsterdam", It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        _listingRepoMock.Verify(x => x.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ShouldUseCache_WhenFresh()
    {
        // Arrange
        var query = new FundaSearchQuery(Region: "amsterdam", OfferingType: "buy");
        
        // First call to populate cache
        _apiClientMock.Setup(x => x.SearchBuyAsync("amsterdam", It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Listing>());
        
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
    public async Task SearchAsync_Rent_ShouldCallSearchRent()
    {
        var query = new FundaSearchQuery(Region: "amsterdam", OfferingType: "rent");
        // Ensure this specific call returns something to distinguish from default
        var expectedListing = new Listing { FundaId = "2", Url = "url", Address = "A" };
        _apiClientMock.Setup(x => x.SearchRentAsync("amsterdam", It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Listing> { expectedListing });

        await _service.SearchAsync(query);

        _apiClientMock.Verify(x => x.SearchRentAsync("amsterdam", It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
        _listingRepoMock.Verify(x => x.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_Projects_ShouldCallSearchProjects()
    {
        var query = new FundaSearchQuery(Region: "amsterdam", OfferingType: "project");
        _apiClientMock.Setup(x => x.SearchProjectsAsync("amsterdam", It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Listing>());

        await _service.SearchAsync(query);

        _apiClientMock.Verify(x => x.SearchProjectsAsync("amsterdam", It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ApiReturnsNull_ShouldLogWarningAndSkipProcessing()
    {
        var query = new FundaSearchQuery(Region: "amsterdam", OfferingType: "buy");
        // Interface returns Task<List>, implementation ensures non-null, but let's say it returns empty list (default setup)

        await _service.SearchAsync(query);

        // Verify warning logged "No listings found"
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No listings found")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task SearchAsync_ProcessListing_SkipInvalidIds()
    {
        var query = new FundaSearchQuery(Region: "amsterdam", OfferingType: "buy");
        _apiClientMock.Setup(x => x.SearchBuyAsync("amsterdam", It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Listing>
            {
                new() { FundaId = "", Url = "url", Address = "A" }, // Empty ID
                new() { FundaId = "1", Url = "", Address = "A" } // Empty URL
            });

        await _service.SearchAsync(query);

        // Should not add any listing
        _listingRepoMock.Verify(x => x.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessListing_ShouldSkipIfFresh()
    {
        var query = new FundaSearchQuery(Region: "amsterdam", OfferingType: "buy");
        var freshTime = DateTime.UtcNow;

        _apiClientMock.Setup(x => x.SearchBuyAsync("amsterdam", It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Listing>
            {
                new() { FundaId = "1", Url = "url", Address = "A" }
            });

        _listingRepoMock.Setup(x => x.GetByFundaIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Listing { FundaId = "1", LastFundaFetchUtc = freshTime, Address = "A" });

        await _service.SearchAsync(query);

        // Should NOT update
        _listingRepoMock.Verify(x => x.UpdateAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessListing_ShouldLogAndContinue_WhenEnrichmentFails()
    {
        var query = new FundaSearchQuery(Region: "amsterdam", OfferingType: "buy");

        _apiClientMock.Setup(x => x.SearchBuyAsync("amsterdam", It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Listing>
            {
                new() { FundaId = "1", Url = "url", Address = "A" }
            });

        // Fail enrichment
        _apiClientMock.Setup(x => x.GetListingDetailsAsync("url", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Enrichment Failed"));

        await _service.SearchAsync(query);

        // Should still add the listing
        _listingRepoMock.Verify(x => x.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Once);
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
            .ReturnsAsync(new Listing
            { 
                FundaId = "42424242",
                Address = "Teststraat",
                City = "Amsterdam"
            });

        // Act
        var result = await _service.GetByFundaUrlAsync(url);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("42424242", result.FundaId);
        _apiClientMock.Verify(x => x.GetListingSummaryAsync(42424242, It.IsAny<CancellationToken>()), Times.Once);
        _listingRepoMock.Verify(x => x.AddAsync(It.IsAny<Listing>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByFundaUrlAsync_InvalidUrl_ReturnsNull()
    {
        var url = "invalid-url";
        var result = await _service.GetByFundaUrlAsync(url);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByFundaUrlAsync_SummaryNotFound_ReturnsExistingOrNull()
    {
        var url = "https://www.funda.nl/42424242/";

        _listingRepoMock.Setup(x => x.GetByFundaIdAsync("42424242", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Listing { FundaId = "42424242", Address = "Stale Address" }); // Stale

        _apiClientMock.Setup(x => x.GetListingSummaryAsync(42424242, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing?)null);

        var result = await _service.GetByFundaUrlAsync(url);

        Assert.NotNull(result); // Returns stale
        // Verify warning
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found on Funda")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetByFundaUrlAsync_ApiError_ReturnsExisting()
    {
        var url = "https://www.funda.nl/42424242/";

        _listingRepoMock.Setup(x => x.GetByFundaIdAsync("42424242", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Listing { FundaId = "42424242", Address = "Existing Address" });

        _apiClientMock.Setup(x => x.GetListingSummaryAsync(42424242, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Fail"));

        var result = await _service.GetByFundaUrlAsync(url);

        Assert.NotNull(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to fetch listing")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
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
