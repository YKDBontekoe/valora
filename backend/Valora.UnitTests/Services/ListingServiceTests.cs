using Moq;
using Xunit;
using Valora.Application.Services;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Application.Common.Models;
using Valora.Application.Common.Exceptions;

namespace Valora.UnitTests.Services;

public class ListingServiceTests
{
    private readonly Mock<IListingRepository> _listingRepositoryMock;
    private readonly Mock<IContextReportService> _contextReportServiceMock;
    private readonly ListingService _sut;

    public ListingServiceTests()
    {
        _listingRepositoryMock = new Mock<IListingRepository>();
        _contextReportServiceMock = new Mock<IContextReportService>();
        _sut = new ListingService(_listingRepositoryMock.Object, _contextReportServiceMock.Object);
    }

    [Fact]
    public async Task GetSummariesAsync_ShouldReturnPaginatedList_WhenFilterIsValid()
    {
        // Arrange
        var filter = new ListingFilterDto { SearchTerm = "Amsterdam", Page = 1, PageSize = 10 };
        var expectedList = new PaginatedList<ListingSummaryDto>(new List<ListingSummaryDto>(), 0, 1, 10);

        _listingRepositoryMock
            .Setup(x => x.GetSummariesAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        // Act
        var result = await _sut.GetSummariesAsync(filter);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(expectedList, result.Value);
    }

    [Fact]
    public async Task GetSummariesAsync_ShouldThrowValidationException_WhenFilterIsInvalid()
    {
        // Arrange
        var filter = new ListingFilterDto { Page = -1 }; // Invalid Page

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _sut.GetSummariesAsync(filter));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnListing_WhenExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var listing = new Listing
        {
            Id = id,
            FundaId = "123",
            Address = "Test St",
            City = "Test City",
            Price = 100000,
            LivingAreaM2 = 50,
            Bedrooms = 2,
            PropertyType = "Apartment",
            Status = "For Sale",
            EnergyLabel = "A"
        };

        _listingRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listing);

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(id, result.Value.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnFailure_WhenNotExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        _listingRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing?)null);

        // Act
        var result = await _sut.GetByIdAsync(id);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Listing not found.", result.Errors);
    }

    [Fact]
    public async Task EnrichListingAsync_ShouldUpdateListing_WhenExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        var listing = new Listing
        {
            Id = id,
            FundaId = "456",
            Address = "Test St",
            City = "Test City"
        };

        var reportDto = new ContextReportDto(
            new ResolvedLocationDto("Test St", "Test St, Test City", 52.0, 4.0, null, null, null, null, null, null, null, null, "1000AA"),
            new List<ContextMetricDto>(),
            new List<ContextMetricDto>(),
            new List<ContextMetricDto>(),
            new List<ContextMetricDto>(),
            new List<ContextMetricDto>(),
            new List<ContextMetricDto>(),
            new List<ContextMetricDto>(),
            85.0,
            new Dictionary<string, double> { { "Safety", 90.0 } },
            new List<SourceAttributionDto>(),
            new List<string>()
        );

        _listingRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listing);

        _contextReportServiceMock
            .Setup(x => x.BuildAsync(It.IsAny<ContextReportRequestDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(reportDto);

        // Act
        var result = await _sut.EnrichListingAsync(id);

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(85.0, result.Value);
        Assert.Equal(85.0, listing.ContextCompositeScore);
        Assert.Equal(90.0, listing.ContextSafetyScore);

        _listingRepositoryMock.Verify(x => x.UpdateAsync(listing, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnrichListingAsync_ShouldReturnFailure_WhenNotExists()
    {
        // Arrange
        var id = Guid.NewGuid();
        _listingRepositoryMock
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing?)null);

        // Act
        var result = await _sut.EnrichListingAsync(id);

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Listing not found.", result.Errors);
        _contextReportServiceMock.Verify(x => x.BuildAsync(It.IsAny<ContextReportRequestDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
