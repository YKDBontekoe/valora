using System.Net;
using System.Net.Http.Json;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

public class ListingDetailTests : BaseIntegrationTest
{
    public ListingDetailTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        await AuthenticateAsync();
    }

    [Fact]
    public async Task Get_ListingDetail_ReturnsProjectedDto()
    {
        // Arrange
        var listing = new Listing
        {
            FundaId = "DetailTest1",
            Address = "Detailed Street 1",
            City = "Amsterdam",
            Price = 750000,
            Bedrooms = 3,
            Bathrooms = 2,
            LivingAreaM2 = 120,
            ImageUrls = new List<string> { "http://img1.com", "http://img2.com" },
            Features = new Dictionary<string, string> { { "Garden", "South" } },
            ContextCompositeScore = 8.5,
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
        Assert.Equal(listing.Id, dto.Id);
        Assert.Equal(listing.Address, dto.Address);
        Assert.Equal(listing.Price, dto.Price);
        Assert.Equal(listing.Bedrooms, dto.Bedrooms);
        Assert.Equal(2, dto.ImageUrls.Count);
        Assert.Equal("http://img1.com", dto.ImageUrls[0]);
        Assert.Equal("South", dto.Features["Garden"]);
        Assert.Equal(8.5, dto.ContextCompositeScore);
    }

    [Fact]
    public async Task Get_ListingDetail_ReturnsNotFound_WhenIdDoesNotExist()
    {
        // Act
        var response = await Client.GetAsync($"/api/listings/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
