using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.Services;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.Services;

public class ListingServiceTests
{
    private readonly Mock<IWorkspaceRepository> _repositoryMock;
    private readonly Mock<IContextDataProvider> _contextDataProviderMock;
    private readonly ListingService _service;

    public ListingServiceTests()
    {
        _repositoryMock = new Mock<IWorkspaceRepository>();
        _contextDataProviderMock = new Mock<IContextDataProvider>();
        _service = new ListingService(_repositoryMock.Object, _contextDataProviderMock.Object);
    }

    [Fact]
    public async Task GetListingDetailAsync_WhenListingDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetListingAsync(listingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetListingDetailAsync(listingId));
    }

    [Fact]
    public async Task GetListingDetailAsync_WhenListingExists_ReturnsDto()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var listing = new Listing
        {
            Id = listingId,
            Address = "Test Address",
            FundaId = "123",
            Price = 100000,
            Bedrooms = 2,
            ContextCompositeScore = 8.5
        };

        _repositoryMock.Setup(r => r.GetListingAsync(listingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listing);

        // Act
        var result = await _service.GetListingDetailAsync(listingId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(listingId);
        result.Address.Should().Be("Test Address");
        result.Price.Should().Be(100000);
        result.Bedrooms.Should().Be(2);
        result.ContextCompositeScore.Should().Be(8.5);
    }
}
