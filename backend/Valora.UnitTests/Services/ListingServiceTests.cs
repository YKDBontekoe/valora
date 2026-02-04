using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Application.Services;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services;

public class ListingServiceTests
{
    private readonly Mock<IListingRepository> _repositoryMock;
    private readonly ListingService _service;

    public ListingServiceTests()
    {
        _repositoryMock = new Mock<IListingRepository>();
        _service = new ListingService(_repositoryMock.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnPaginatedList()
    {
        // Arrange
        var filter = new ListingFilterDto();
        var dto = new ListingDto(
            Guid.NewGuid(), "1", "Address", "City", null, null, null, null, null, null,
            null, null, null, null, null, DateTime.UtcNow,
            // Rich Data
            null, null, null, new(),
            // Phase 2
            null, null, null, null, null, null, false, null,
            // Phase 3
            null, null, null, null, null, new(),
            // Geo & Media
            null, null, null, null, new(), null,
            // Construction
            null, null, null, null, null,
            // Broker
            null, null,
            // Infra
            null,
            // Status
            null, false, new List<string>()
        );
        var expectedItems = new List<ListingDto> { dto };
        var expectedPaginatedList = new PaginatedList<ListingDto>(expectedItems, 1, 1, 1);

        _repositoryMock.Setup(r => r.GetAllAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedPaginatedList);

        // Act
        var result = await _service.GetAllAsync(filter, CancellationToken.None);

        // Assert
        Assert.Equal(expectedPaginatedList, result);
        _repositoryMock.Verify(r => r.GetAllAsync(filter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenListingExists_ShouldReturnDto()
    {
        // Arrange
        var id = Guid.NewGuid();
        var listing = new Listing
        {
            Id = id,
            FundaId = "123",
            Address = "Test Address",
            City = "Test City",
            Price = 500000,
            CreatedAt = DateTime.UtcNow,
            ImageUrls = new List<string>(),
            Features = new Dictionary<string, string>(),
            Labels = new List<string>(),
            FloorPlanUrls = new List<string>()
        };

        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listing);

        // Act
        var result = await _service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("Test Address", result.Address);
        _repositoryMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenListingDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing?)null);

        // Act
        var result = await _service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        Assert.Null(result);
        _repositoryMock.Verify(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()), Times.Once);
    }
}
