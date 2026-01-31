using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Services;
using Xunit;

namespace Valora.UnitTests.Services;

public class ListingServiceTests
{
    private readonly Mock<IListingRepository> _mockRepository;
    private readonly ListingService _service;

    public ListingServiceTests()
    {
        _mockRepository = new Mock<IListingRepository>();
        _service = new ListingService(_mockRepository.Object);
    }

    [Fact]
    public async Task GetListingsAsync_ShouldReturnMappedDtos()
    {
        // Arrange
        var filter = new ListingFilterDto();
        var listings = new List<Listing>
        {
            new Listing
            {
                Id = Guid.NewGuid(),
                FundaId = "1",
                Address = "Test Address",
                City = "Amsterdam",
                Price = 500000,
                ListedDate = DateTime.UtcNow
            }
        };
        var paginatedList = new PaginatedList<Listing>(listings, 1, 1, 10);

        _mockRepository
            .Setup(r => r.GetAllAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedList);

        // Act
        var result = await _service.GetListingsAsync(filter);

        // Assert
        Assert.Single(result.Items);
        var dto = result.Items[0];
        Assert.Equal("1", dto.FundaId);
        Assert.Equal("Test Address", dto.Address);
        Assert.Equal("Amsterdam", dto.City);
        Assert.Equal(500000, dto.Price);

        // Verify Pagination mapping
        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, result.PageIndex);
    }

    [Fact]
    public async Task GetListingByIdAsync_ShouldReturnDto_WhenFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        var listing = new Listing
        {
            Id = id,
            FundaId = "1",
            Address = "Test Address",
            City = "Amsterdam",
            Price = 500000
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listing);

        // Act
        var result = await _service.GetListingByIdAsync(id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
        Assert.Equal("Test Address", result.Address);
    }

    [Fact]
    public async Task GetListingByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository
            .Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing?)null);

        // Act
        var result = await _service.GetListingByIdAsync(id);

        // Assert
        Assert.Null(result);
    }
}
