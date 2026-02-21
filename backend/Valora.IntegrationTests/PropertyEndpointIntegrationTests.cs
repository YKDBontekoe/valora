using System.Net;
using System.Net.Http.Json;
using Valora.Application.DTOs.Map;
using Valora.Application.DTOs.Property;
using Valora.Domain.Entities;
using Valora.IntegrationTests;

namespace Valora.IntegrationTests;

public class PropertyEndpointIntegrationTests : BaseIntegrationTest
{
    public PropertyEndpointIntegrationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetPropertyDetail_ShouldReturnDetails_WhenExists()
    {
        // Arrange
        await AuthenticateAsync();
        var listing = new Listing
        {
            Id = Guid.NewGuid(),
            FundaId = "12345",
            Address = "Test Street 1",
            City = "Amsterdam",
            Price = 500000,
            LivingAreaM2 = 100,
            Bedrooms = 2,
            Latitude = 52.3702,
            Longitude = 4.8952,
            Status = "Sold",
            ContextCompositeScore = 8.5
        };
        DbContext.Listings.Add(listing);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/properties/{listing.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PropertyDetailDto>();
        Assert.NotNull(result);
        Assert.Equal(listing.Id, result.Id);
        Assert.Equal(listing.Address, result.Address);
        Assert.Equal(listing.Price, result.Price);
    }

    [Fact]
    public async Task GetPropertyDetail_ShouldReturnNotFound_WhenIdInvalid()
    {
        // Arrange
        await AuthenticateAsync();
        var nonExistentId = Guid.NewGuid();

        // Act
        var response = await Client.GetAsync($"/api/properties/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetMapProperties_ShouldReturnPropertiesInBbox()
    {
        // Arrange
        await AuthenticateAsync();
        var listing1 = new Listing
        {
            Id = Guid.NewGuid(),
            FundaId = "A",
            Address = "In Bbox",
            Price = 300000,
            LivingAreaM2 = 80,
            Latitude = 52.3702, // Center
            Longitude = 4.8952
        };
        var listing2 = new Listing
        {
            Id = Guid.NewGuid(),
            FundaId = "B",
            Address = "Out Bbox",
            Price = 400000,
            LivingAreaM2 = 90,
            Latitude = 50.0, // Far away
            Longitude = 4.0
        };
        DbContext.Listings.AddRange(listing1, listing2);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/map/properties?minLat=52.3&minLon=4.8&maxLat=52.4&maxLon=4.9");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MapPropertyDto>>();
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(listing1.Id, result[0].Id);
    }
}
