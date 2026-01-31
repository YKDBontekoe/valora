using System.Net;
using System.Net.Http.Json;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Valora.UnitTests.EndpointTests;

public class ListingEndpointTests : IClassFixture<EndpointTestFactory>
{
    private readonly EndpointTestFactory _factory;

    public ListingEndpointTests(EndpointTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Get_Listings_ReturnsOk()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            // Ensure clean state
            context.Database.EnsureDeleted();
            context.Listings.Add(new Listing {
                FundaId = "1",
                Address = "Test",
                City = "Amsterdam",
                PostalCode = "1000AA",
                CreatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/listings");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_ListingById_ReturnsOk_WhenExists()
    {
        Guid id;
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            context.Database.EnsureDeleted();
            var listing = new Listing {
                FundaId = "2",
                Address = "Test 2",
                City = "Rotterdam",
                PostalCode = "2000BB",
                CreatedAt = DateTime.UtcNow
            };
            context.Listings.Add(listing);
            await context.SaveChangesAsync();
            id = listing.Id;
        }

        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/listings/{id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Get_ListingById_ReturnsNotFound_WhenMissing()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync($"/api/listings/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
