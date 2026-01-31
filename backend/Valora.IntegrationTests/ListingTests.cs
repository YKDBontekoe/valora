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
        var result = await response.Content.ReadFromJsonAsync<ListingResponseDto>();
        Assert.NotNull(result);
        Assert.NotNull(result.Items);
        Assert.NotEmpty(result.Items);
        Assert.Contains(result.Items, l => l.FundaId == "12345");
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

    [Fact]
    public async Task Get_Listings_WithCityFilter_ReturnsCorrectListings()
    {
        // Arrange
        var listing1 = new Listing { FundaId = "Filter1", Address = "A", City = "Utrecht" };
        var listing2 = new Listing { FundaId = "Filter2", Address = "B", City = "Rotterdam" };
        DbContext.Listings.AddRange(listing1, listing2);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/listings?city=Utrecht");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListingResponseDto>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("Utrecht", result.Items.First().City);
    }

    [Fact]
    public async Task Get_Listings_WithSearchTerm_ReturnsCorrectListings()
    {
        // Arrange
        var listing1 = new Listing { FundaId = "Search1", Address = "UniqueSearchTerm Street", City = "CityA" };
        var listing2 = new Listing { FundaId = "Search2", Address = "Another Road", City = "CityB" };
        DbContext.Listings.AddRange(listing1, listing2);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/listings?searchTerm=unique");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListingResponseDto>();
        Assert.NotNull(result);
        Assert.Single(result.Items);
        Assert.Equal("UniqueSearchTerm Street", result.Items.First().Address);
    }

    [Fact]
    public async Task Get_Listings_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        for (int i = 0; i < 15; i++)
        {
             DbContext.Listings.Add(new Listing
             {
                 FundaId = $"Page_{i}",
                 Address = $"Street {i}",
                 City = "Amsterdam",
                 ListedDate = DateTime.UtcNow
             });
        }
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/listings?page=1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ListingResponseDto>();
        Assert.NotNull(result);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(1, result.PageIndex);
        Assert.True(result.HasNextPage);

        // Act Page 2
        var response2 = await Client.GetAsync("/api/listings?page=2&pageSize=10");
        var result2 = await response2.Content.ReadFromJsonAsync<ListingResponseDto>();

        Assert.Equal(5, result2.Items.Count);
        Assert.False(result2.HasNextPage);
    }
}

public class ListingResponseDto
{
    public List<ListingDto> Items { get; set; } = new();
    public int PageIndex { get; set; }
    public int TotalPages { get; set; }
    public int TotalCount { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
