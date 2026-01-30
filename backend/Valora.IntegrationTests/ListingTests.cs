using System.Net.Http.Json;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

public class ListingTests : BaseIntegrationTest
{
    public ListingTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Get_Listings_ReturnsListings()
    {
        // Arrange
        var listing = new Listing
        {
            FundaId = "12345",
            Address = "Test Street 1",
            City = "Amsterdam",
            Price = 500000,
            ListedDate = DateTime.UtcNow
        };
        DbContext.Listings.Add(listing);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/listings");
        
        // Assert
        response.EnsureSuccessStatusCode();
        var listings = await response.Content.ReadFromJsonAsync<List<ListingDto>>();
        Assert.NotNull(listings);
        Assert.NotEmpty(listings);
        Assert.Contains(listings, l => l.FundaId == "12345");
    }

    [Fact]
    public async Task Get_ListingById_ReturnsListing()
    {
        // Arrange
        var listing = new Listing
        {
            FundaId = "67890",
            Address = "Test Lane 2",
            City = "Rotterdam",
            Price = 300000,
            ListedDate = DateTime.UtcNow
        };
        DbContext.Listings.Add(listing);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/listings/{listing.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<ListingDto>();
        Assert.NotNull(dto);
        Assert.Equal(listing.FundaId, dto.FundaId);
    }

    [Fact]
    public async Task CountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var repository = GetRequiredService<IListingRepository>();
        var listing1 = new Listing { FundaId = "1", Address = "A" };
        var listing2 = new Listing { FundaId = "2", Address = "B" };

        await repository.AddAsync(listing1);
        await repository.AddAsync(listing2);

        // Act
        var count = await repository.CountAsync();

        // Assert
        Assert.Equal(2, count);
    }
}
