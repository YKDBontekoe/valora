using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Services;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services;

public class ListingServiceTests
{
    private readonly Mock<IListingRepository> _listingRepositoryMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepositoryMock;
    private readonly Mock<ILogger<ListingService>> _loggerMock;
    private readonly ListingService _sut;

    public ListingServiceTests()
    {
        _listingRepositoryMock = new Mock<IListingRepository>();
        _priceHistoryRepositoryMock = new Mock<IPriceHistoryRepository>();
        _loggerMock = new Mock<ILogger<ListingService>>();

        _sut = new ListingService(
            _listingRepositoryMock.Object,
            _priceHistoryRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateListingAsync_ShouldAddListingAndPriceHistory()
    {
        // Arrange
        var listing = new Listing
        {
            FundaId = "123",
            Address = "Test Address",
            Price = 500000
        };

        // Act
        await _sut.CreateListingAsync(listing);

        // Assert
        _listingRepositoryMock.Verify(x => x.AddAsync(listing, It.IsAny<CancellationToken>()), Times.Once);
        _priceHistoryRepositoryMock.Verify(x => x.AddAsync(
            It.Is<PriceHistory>(ph => ph.ListingId == listing.Id && ph.Price == 500000),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal("Beschikbaar", listing.Status); // Should set default status
    }

    [Fact]
    public async Task CreateListingAsync_ShouldNotAddPriceHistory_WhenPriceIsNull()
    {
        // Arrange
        var listing = new Listing
        {
            FundaId = "123",
            Address = "Test Address",
            Price = null
        };

        // Act
        await _sut.CreateListingAsync(listing);

        // Assert
        _listingRepositoryMock.Verify(x => x.AddAsync(listing, It.IsAny<CancellationToken>()), Times.Once);
        _priceHistoryRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PriceHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateListingAsync_ShouldAddPriceHistory_WhenPriceChanged()
    {
        // Arrange
        var existingListing = new Listing
        {
            Id = Guid.NewGuid(),
            FundaId = "123",
            Address = "Test Address",
            Price = 500000,
            Status = "Beschikbaar"
        };

        var newListing = new Listing
        {
            FundaId = "123",
            Address = "Test Address",
            Price = 550000 // Price increased
        };

        // Act
        await _sut.UpdateListingAsync(existingListing, newListing);

        // Assert
        _listingRepositoryMock.Verify(x => x.UpdateAsync(existingListing, It.IsAny<CancellationToken>()), Times.Once);
        _priceHistoryRepositoryMock.Verify(x => x.AddAsync(
            It.Is<PriceHistory>(ph => ph.ListingId == existingListing.Id && ph.Price == 550000),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.Equal(550000, existingListing.Price);
    }

    [Fact]
    public async Task UpdateListingAsync_ShouldNotAddPriceHistory_WhenPriceUnchanged()
    {
        // Arrange
        var existingListing = new Listing
        {
            Id = Guid.NewGuid(),
            FundaId = "123",
            Address = "Test Address",
            Price = 500000
        };

        var newListing = new Listing
        {
            FundaId = "123",
            Address = "Test Address",
            Price = 500000
        };

        // Act
        await _sut.UpdateListingAsync(existingListing, newListing);

        // Assert
        _listingRepositoryMock.Verify(x => x.UpdateAsync(existingListing, It.IsAny<CancellationToken>()), Times.Once);
        _priceHistoryRepositoryMock.Verify(x => x.AddAsync(It.IsAny<PriceHistory>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateListingAsync_ShouldMergeDetails()
    {
        // Arrange
        var existingListing = new Listing
        {
            FundaId = "123",
            Address = "Test Address",
            Bedrooms = 2,
            LivingAreaM2 = 100
        };

        var newListing = new Listing
        {
            FundaId = "123",
            Address = "Test Address",
            Bedrooms = 3, // Changed
            LivingAreaM2 = null // Missing in new data
        };

        // Act
        await _sut.UpdateListingAsync(existingListing, newListing);

        // Assert
        Assert.Equal(3, existingListing.Bedrooms);
        Assert.Equal(100, existingListing.LivingAreaM2); // Should preserve existing value
        _listingRepositoryMock.Verify(x => x.UpdateAsync(existingListing, It.IsAny<CancellationToken>()), Times.Once);
    }
}
