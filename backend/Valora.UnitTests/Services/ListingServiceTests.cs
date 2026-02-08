using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Services;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services;

public class ListingServiceTests
{
    private readonly Mock<IListingRepository> _listingRepoMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepoMock;
    private readonly Mock<ILogger<ListingService>> _loggerMock;
    private readonly ListingService _service;

    public ListingServiceTests()
    {
        _listingRepoMock = new Mock<IListingRepository>();
        _priceHistoryRepoMock = new Mock<IPriceHistoryRepository>();
        _loggerMock = new Mock<ILogger<ListingService>>();

        _service = new ListingService(
            _listingRepoMock.Object,
            _priceHistoryRepoMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task ProcessListingAsync_NewListing_ShouldAddListingAndPriceHistory()
    {
        // Arrange
        var newListing = new Listing { FundaId = "1", Address = "Addr", Price = 100000 };

        _listingRepoMock.Setup(x => x.GetByFundaIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing?)null);

        // Act
        var result = await _service.ProcessListingAsync(newListing);

        // Assert
        Assert.True(result.IsNew);
        Assert.False(result.IsUpdated);
        Assert.Equal(newListing, result.Listing);

        _listingRepoMock.Verify(x => x.AddAsync(newListing, It.IsAny<CancellationToken>()), Times.Once);
        _priceHistoryRepoMock.Verify(x => x.AddAsync(It.Is<PriceHistory>(ph => ph.Price == 100000), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessListingAsync_ExistingListing_ShouldUpdateAndMerge()
    {
        // Arrange
        var newListing = new Listing { FundaId = "1", Address = "Addr", Price = 100000, Bedrooms = 3 };
        var existingListing = new Listing { FundaId = "1", Address = "Addr", Price = 100000, Bedrooms = 2 };

        // Act
        var result = await _service.ProcessListingAsync(newListing, existingListing);

        // Assert
        Assert.False(result.IsNew);
        Assert.True(result.IsUpdated);
        Assert.Same(existingListing, result.Listing);
        Assert.Equal(3, existingListing.Bedrooms); // Merged

        _listingRepoMock.Verify(x => x.UpdateAsync(existingListing, It.IsAny<CancellationToken>()), Times.Once);
        // Price didn't change
        _priceHistoryRepoMock.Verify(x => x.AddAsync(It.IsAny<PriceHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessListingAsync_PriceChange_ShouldAddPriceHistory()
    {
        // Arrange
        var newListing = new Listing { FundaId = "1", Address = "Addr", Price = 200000 };
        var existingListing = new Listing { Id = Guid.NewGuid(), FundaId = "1", Address = "Addr", Price = 100000 };

        // Act
        var result = await _service.ProcessListingAsync(newListing, existingListing);

        // Assert
        Assert.False(result.IsNew);
        Assert.True(result.IsUpdated);
        Assert.Equal(200000, existingListing.Price);

        _listingRepoMock.Verify(x => x.UpdateAsync(existingListing, It.IsAny<CancellationToken>()), Times.Once);
        _priceHistoryRepoMock.Verify(x => x.AddAsync(It.Is<PriceHistory>(ph => ph.Price == 200000 && ph.ListingId == existingListing.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessListingAsync_ExistingNotProvided_ShouldFetchFromRepo()
    {
        // Arrange
        var newListing = new Listing { FundaId = "1", Address = "Addr" };
        var existingListing = new Listing { FundaId = "1", Address = "Addr" };

        _listingRepoMock.Setup(x => x.GetByFundaIdAsync("1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingListing);

        // Act
        var result = await _service.ProcessListingAsync(newListing);

        // Assert
        Assert.False(result.IsNew);
        Assert.True(result.IsUpdated);
        Assert.Same(existingListing, result.Listing);

        _listingRepoMock.Verify(x => x.GetByFundaIdAsync("1", It.IsAny<CancellationToken>()), Times.Once);
        _listingRepoMock.Verify(x => x.UpdateAsync(existingListing, It.IsAny<CancellationToken>()), Times.Once);
    }
}
