using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;
using Valora.Application.Services;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services;

public class ListingQueryServiceTests
{
    [Fact]
    public async Task GetListingByIdAsync_ReturnsMappedDto_WhenListingExists()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var createdAt = new DateTime(2024, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var listing = new Listing
        {
            Id = listingId,
            FundaId = "funda-123",
            Address = "123 Main St",
            City = "Utrecht",
            PostalCode = "1234AB",
            Price = 450000,
            Bedrooms = 2,
            Bathrooms = 1,
            LivingAreaM2 = 80,
            PlotAreaM2 = 120,
            PropertyType = "Apartment",
            Status = "For Sale",
            Url = "https://example.com/listing",
            ImageUrl = "https://example.com/image",
            ListedDate = new DateTime(2024, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            CreatedAt = createdAt
        };

        var repository = new Mock<IListingRepository>();
        repository.Setup(repo => repo.GetByIdAsync(listingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(listing);

        var service = new ListingQueryService(repository.Object);

        // Act
        var dto = await service.GetListingByIdAsync(listingId);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(listing.Id, dto.Id);
        Assert.Equal(listing.FundaId, dto.FundaId);
        Assert.Equal(listing.Address, dto.Address);
        Assert.Equal(listing.City, dto.City);
        Assert.Equal(listing.PostalCode, dto.PostalCode);
        Assert.Equal(listing.Price, dto.Price);
        Assert.Equal(listing.Bedrooms, dto.Bedrooms);
        Assert.Equal(listing.Bathrooms, dto.Bathrooms);
        Assert.Equal(listing.LivingAreaM2, dto.LivingAreaM2);
        Assert.Equal(listing.PlotAreaM2, dto.PlotAreaM2);
        Assert.Equal(listing.PropertyType, dto.PropertyType);
        Assert.Equal(listing.Status, dto.Status);
        Assert.Equal(listing.Url, dto.Url);
        Assert.Equal(listing.ImageUrl, dto.ImageUrl);
        Assert.Equal(listing.ListedDate, dto.ListedDate);
        Assert.Equal(listing.CreatedAt, dto.CreatedAt);
    }

    [Fact]
    public async Task GetListingByIdAsync_ReturnsNull_WhenListingDoesNotExist()
    {
        // Arrange
        var listingId = Guid.NewGuid();
        var repository = new Mock<IListingRepository>();
        repository.Setup(repo => repo.GetByIdAsync(listingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Listing?)null);

        var service = new ListingQueryService(repository.Object);

        // Act
        var dto = await service.GetListingByIdAsync(listingId);

        // Assert
        Assert.Null(dto);
    }

    [Fact]
    public async Task GetListingsAsync_ReturnsMappedPaginatedListings()
    {
        // Arrange
        var filter = new ListingFilterDto { Page = 1, PageSize = 10 };
        var listings = new List<Listing>
        {
            new()
            {
                Id = Guid.NewGuid(),
                FundaId = "f1",
                Address = "Address 1",
                City = "Amsterdam",
                Price = 100000,
                CreatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                FundaId = "f2",
                Address = "Address 2",
                City = "Rotterdam",
                Price = 200000,
                CreatedAt = DateTime.UtcNow
            }
        };
        var paginatedList = new PaginatedList<Listing>(listings, listings.Count, 1, 10);

        var repository = new Mock<IListingRepository>();
        repository.Setup(repo => repo.GetAllAsync(filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedList);

        var service = new ListingQueryService(repository.Object);

        // Act
        var result = await service.GetListingsAsync(filter);

        // Assert
        Assert.Equal(paginatedList.TotalCount, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.PageIndex);
        Assert.Equal("f1", result.Items[0].FundaId);
        Assert.Equal("Address 2", result.Items[1].Address);
    }
}
