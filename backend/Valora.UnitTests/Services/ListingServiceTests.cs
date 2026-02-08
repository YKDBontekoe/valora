using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Services;
using Valora.Domain.Entities;
using Xunit;

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
    public async Task AddNewListingAsync_ShouldNotifyWithFundaId()
    {
        // Arrange
        var listing = new Listing { FundaId = "123", Address = "Test Address", Price = 500000 };

        // Act
        await _service.SaveListingAsync(listing, null, true, CancellationToken.None);

        // Assert
        // Verify notification uses FundaId instead of Address
        _notificationServiceMock.Verify(x => x.NotifyListingFoundAsync("123"), Times.Once);
        _notificationServiceMock.Verify(x => x.NotifyListingFoundAsync("Test Address"), Times.Never);
    }

    [Fact]
    public async Task UpdateExistingListingAsync_ShouldNotifyWithFundaId()
    {
        // Arrange
        var existing = new Listing { Id = Guid.NewGuid(), FundaId = "123", Address = "Old Address", Price = 400000 };
        var updated = new Listing { FundaId = "123", Address = "New Address", Price = 450000 };

        // Act
        await _service.SaveListingAsync(updated, existing, true, CancellationToken.None);

        // Assert
        // Verify notification uses FundaId
        _notificationServiceMock.Verify(x => x.NotifyListingFoundAsync("123"), Times.Once);
        // Address should not be used
        _notificationServiceMock.Verify(x => x.NotifyListingFoundAsync(It.Is<string>(s => s.Contains("Address"))), Times.Never);
    }

    [Fact]
    public async Task AddNewListingAsync_ShouldSetLastFundaFetchUtc()
    {
        // Arrange
        var listing = new Listing { FundaId = "123", Address = "Address" };

        // Act
        await _service.SaveListingAsync(listing, null, false, CancellationToken.None);

        // Assert
        Assert.NotNull(listing.LastFundaFetchUtc);
        // Should be recent (within 1 second)
        Assert.True((DateTime.UtcNow - listing.LastFundaFetchUtc.Value).TotalSeconds < 1);
    }

    [Fact]
    public async Task NotifyMatchFoundAsync_ShouldPropagateCancellation()
    {
        // Arrange
        var listing = new Listing { FundaId = "123", Address = "Address" };
        _notificationServiceMock.Setup(x => x.NotifyListingFoundAsync(It.IsAny<string>()))
            .ThrowsAsync(new OperationCanceledException());

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            _service.SaveListingAsync(listing, null, true, CancellationToken.None));
    }
}
