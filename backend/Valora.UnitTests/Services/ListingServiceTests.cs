using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Services;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services;

public class ListingServiceTests
{
    private readonly Mock<IListingRepository> _listingRepoMock;
    private readonly Mock<IPriceHistoryRepository> _priceHistoryRepoMock;
    private readonly Mock<IScraperNotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<ListingService>> _loggerMock;
    private readonly ListingService _service;

    public ListingServiceTests()
    {
        _listingRepoMock = new Mock<IListingRepository>();
        _priceHistoryRepoMock = new Mock<IPriceHistoryRepository>();
        _notificationServiceMock = new Mock<IScraperNotificationService>();
        _loggerMock = new Mock<ILogger<ListingService>>();

        _service = new ListingService(
            _listingRepoMock.Object,
            _priceHistoryRepoMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task ProcessListingsAsync_ShouldAddNewListing_WhenNotExists()
    {
        // Arrange
        var dto = new ScrapedListingDto
        {
            FundaId = "123",
            Url = "http://url",
            Price = 500000,
            Address = "New Addr",
            City = "City"
        };

        _listingRepoMock.Setup(x => x.GetByFundaIdAsync("123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing?)null);

        // Act
        await _service.ProcessListingsAsync([dto], true);

        // Assert
        _listingRepoMock.Verify(x => x.AddAsync(It.Is<Listing>(l => l.FundaId == "123" && l.Price == 500000), It.IsAny<CancellationToken>()), Times.Once);
        _priceHistoryRepoMock.Verify(x => x.AddAsync(It.IsAny<PriceHistory>(), It.IsAny<CancellationToken>()), Times.Once); // Initial price
        _notificationServiceMock.Verify(x => x.NotifyListingFoundAsync("New Addr"), Times.Once);
    }

    [Fact]
    public async Task ProcessListingsAsync_ShouldUpdateListing_AndAddPriceHistory_WhenPriceChanged()
    {
        // Arrange
        var existingListing = new Listing
        {
            Id = Guid.NewGuid(),
            FundaId = "123",
            Price = 450000,
            Address = "Old Addr"
        };

        var dto = new ScrapedListingDto
        {
            FundaId = "123",
            Url = "http://url",
            Price = 500000, // Price increased
            Address = "New Addr"
        };

        _listingRepoMock.Setup(x => x.GetByFundaIdAsync("123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingListing);

        // Act
        await _service.ProcessListingsAsync([dto], true);

        // Assert
        _listingRepoMock.Verify(x => x.UpdateAsync(It.Is<Listing>(l => l.Price == 500000), It.IsAny<CancellationToken>()), Times.Once);
        _priceHistoryRepoMock.Verify(x => x.AddAsync(It.Is<PriceHistory>(ph => ph.ListingId == existingListing.Id && ph.Price == 500000), It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(x => x.NotifyListingFoundAsync(It.Is<string>(s => s.Contains("(Updated)"))), Times.Once);
    }

    [Fact]
    public async Task ProcessListingsAsync_ShouldNotOverwrite_WithNulls()
    {
        // Arrange
        var existingListing = new Listing
        {
            Id = Guid.NewGuid(),
            FundaId = "123",
            Address = "Some Addr",
            Price = 500000,
            ImageUrl = "http://img",
            Bedrooms = 3,
            LivingAreaM2 = 100
        };

        var dto = new ScrapedListingDto
        {
            FundaId = "123",
            Url = "http://url",
            Price = null, // Should not overwrite
            ImageUrl = null, // Should not overwrite
            Bedrooms = null, // Should not overwrite
            LivingAreaM2 = null // Should not overwrite
        };

        _listingRepoMock.Setup(x => x.GetByFundaIdAsync("123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingListing);

        // Act
        await _service.ProcessListingsAsync([dto], false);

        // Assert
        _listingRepoMock.Verify(x => x.UpdateAsync(It.Is<Listing>(l =>
            l.Price == 500000 &&
            l.ImageUrl == "http://img" &&
            l.Bedrooms == 3 &&
            l.LivingAreaM2 == 100), It.IsAny<CancellationToken>()), Times.Once);
    }
}
