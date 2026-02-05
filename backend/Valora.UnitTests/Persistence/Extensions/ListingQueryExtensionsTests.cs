using Xunit;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence.Extensions;

namespace Valora.UnitTests.Persistence.Extensions;

public class ListingQueryExtensionsTests
{
    private readonly IQueryable<Listing> _listings;

    public ListingQueryExtensionsTests()
    {
        _listings = new List<Listing>
        {
            new() { Id = Guid.NewGuid(), FundaId = "1", Address = "A1", Latitude = 10, Longitude = 10 },
            new() { Id = Guid.NewGuid(), FundaId = "2", Address = "A2", Latitude = 20, Longitude = 20 },
            new() { Id = Guid.NewGuid(), FundaId = "3", Address = "A3", Latitude = 30, Longitude = 30 },
        }.AsQueryable();
    }

    [Fact]
    public void ApplyBoundingBoxFilter_ShouldReturnAll_WhenFilterIsEmpty()
    {
        // Arrange
        var filter = new ListingFilterDto();

        // Act
        var result = _listings.ApplyBoundingBoxFilter(filter);

        // Assert
        Assert.Equal(3, result.Count());
    }

    [Fact]
    public void ApplyBoundingBoxFilter_ShouldFilterByMinLat()
    {
        // Arrange
        var filter = new ListingFilterDto { MinLat = 15 };

        // Act
        var result = _listings.ApplyBoundingBoxFilter(filter);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.True(result.All(l => l.Latitude >= 15));
    }

    [Fact]
    public void ApplyBoundingBoxFilter_ShouldFilterByMaxLat()
    {
        // Arrange
        var filter = new ListingFilterDto { MaxLat = 25 };

        // Act
        var result = _listings.ApplyBoundingBoxFilter(filter);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.True(result.All(l => l.Latitude <= 25));
    }

    [Fact]
    public void ApplyBoundingBoxFilter_ShouldFilterByMinLng()
    {
        // Arrange
        var filter = new ListingFilterDto { MinLng = 15 };

        // Act
        var result = _listings.ApplyBoundingBoxFilter(filter);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.True(result.All(l => l.Longitude >= 15));
    }

    [Fact]
    public void ApplyBoundingBoxFilter_ShouldFilterByMaxLng()
    {
        // Arrange
        var filter = new ListingFilterDto { MaxLng = 25 };

        // Act
        var result = _listings.ApplyBoundingBoxFilter(filter);

        // Assert
        Assert.Equal(2, result.Count());
        Assert.True(result.All(l => l.Longitude <= 25));
    }

    [Fact]
    public void ApplyBoundingBoxFilter_ShouldFilterByBox()
    {
        // Arrange
        var filter = new ListingFilterDto
        {
            MinLat = 15,
            MaxLat = 25,
            MinLng = 15,
            MaxLng = 25
        };

        // Act
        var result = _listings.ApplyBoundingBoxFilter(filter);

        // Assert
        Assert.Single(result);
        var item = result.First();
        Assert.Equal(20, item.Latitude);
        Assert.Equal(20, item.Longitude);
    }
}
