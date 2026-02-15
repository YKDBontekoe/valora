using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.DTOs.Map;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class MapInsightsIntegrationTests : IAsyncLifetime
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private HttpClient Client = null!;
    private ValoraDbContext DbContext = null!;
    private IServiceScope _scope = null!;

    public MapInsightsIntegrationTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        if (_fixture.Factory == null)
        {
             await _fixture.InitializeAsync();
        }

        Client = _fixture.Factory!.CreateClient();
        _scope = _fixture.Factory.Services.CreateScope();
        DbContext = _scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        // Cleanup
        DbContext.Listings.RemoveRange(DbContext.Listings);
        DbContext.RefreshTokens.RemoveRange(DbContext.RefreshTokens);
        if (DbContext.Users.Any())
        {
            DbContext.Users.RemoveRange(DbContext.Users);
        }
        await DbContext.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        _scope?.Dispose();
        await Task.CompletedTask;
    }

    private async Task AuthenticateAsync(string email = "test@example.com", string password = "Password123!")
    {
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });
        // We allow register to fail (e.g. user already exists) but login must succeed

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (authResponse?.Token == null)
        {
            throw new InvalidOperationException("Failed to extract auth token from login response");
        }
        Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
    }

    [Fact]
    public async Task GetCityInsights_ReturnsCorrectAggregates()
    {
        // Arrange
        // Seed listings
        var listingA1 = new Listing
        {
            FundaId = "1001",
            City = "CityA",
            Latitude = 52.0,
            Longitude = 4.0,
            ContextCompositeScore = 80,
            ContextSafetyScore = 90,
            ContextSocialScore = 70,
            ContextAmenitiesScore = 60,
            Address = "Address A1",
            ListedDate = DateTime.UtcNow
        };
        var listingA2 = new Listing
        {
            FundaId = "1002",
            City = "CityA",
            Latitude = 52.2,
            Longitude = 4.2,
            ContextCompositeScore = 60,
            ContextSafetyScore = 70,
            ContextSocialScore = 50,
            ContextAmenitiesScore = 40,
            Address = "Address A2",
            ListedDate = DateTime.UtcNow
        };
        var listingB1 = new Listing
        {
            FundaId = "2001",
            City = "CityB",
            Latitude = 53.0,
            Longitude = 5.0,
            ContextCompositeScore = 50,
            ContextSafetyScore = 60,
            ContextSocialScore = 40,
            ContextAmenitiesScore = 30,
            Address = "Address B1",
            ListedDate = DateTime.UtcNow
        };
        var listingC1 = new Listing
        {
            FundaId = "3001",
            City = "CityC",
            Latitude = 51.0,
            Longitude = 3.0,
            ContextCompositeScore = null,
            ContextSafetyScore = null,
            ContextSocialScore = null,
            ContextAmenitiesScore = null,
            Address = "Address C1",
            ListedDate = DateTime.UtcNow
        };
        var listingInvalid1 = new Listing // No city
        {
            FundaId = "4001",
            City = null,
            Latitude = 50.0,
            Longitude = 2.0,
            Address = "Address Invalid1",
            ListedDate = DateTime.UtcNow
        };
        var listingInvalid2 = new Listing // No coords
        {
            FundaId = "4002",
            City = "CityD",
            Latitude = null,
            Longitude = null,
            Address = "Address Invalid2",
            ListedDate = DateTime.UtcNow
        };

        DbContext.Listings.AddRange(listingA1, listingA2, listingB1, listingC1, listingInvalid1, listingInvalid2);
        await DbContext.SaveChangesAsync();

        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/map/cities");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MapCityInsightDto>>();
        Assert.NotNull(result);

        // Should return CityA, CityB, CityC
        Assert.Equal(3, result.Count);

        // Verify CityA
        var cityA = result.FirstOrDefault(x => x.City == "CityA");
        Assert.NotNull(cityA);
        Assert.Equal(2, cityA.Count);
        Assert.Equal(52.1, cityA.Latitude, 1); // (52.0 + 52.2) / 2
        Assert.Equal(4.1, cityA.Longitude, 1); // (4.0 + 4.2) / 2
        Assert.Equal(70, cityA.CompositeScore.GetValueOrDefault(), 1);
        Assert.Equal(80, cityA.SafetyScore.GetValueOrDefault(), 1);
        Assert.Equal(60, cityA.SocialScore.GetValueOrDefault(), 1);
        Assert.Equal(50, cityA.AmenitiesScore.GetValueOrDefault(), 1);

        // Verify CityB
        var cityB = result.FirstOrDefault(x => x.City == "CityB");
        Assert.NotNull(cityB);
        Assert.Equal(1, cityB.Count);
        Assert.Equal(53.0, cityB.Latitude);
        Assert.Equal(5.0, cityB.Longitude);
        Assert.Equal(50, cityB.CompositeScore);

        // Verify CityC (null scores)
        var cityC = result.FirstOrDefault(x => x.City == "CityC");
        Assert.NotNull(cityC);
        Assert.Equal(1, cityC.Count);
        // Expecting null if all values are null
        Assert.Null(cityC.CompositeScore);
        Assert.Null(cityC.SafetyScore);

        // Verify invalid listings are not present
        Assert.DoesNotContain(result, x => x.City == "CityD");
    }

    [Fact]
    public async Task GetPriceOverlays_CalculatesAveragePrice()
    {
        // Arrange
        var minLat = 52.0;
        var minLon = 4.0;
        var maxLat = 52.1;
        var maxLon = 4.1;

        // Seed listings within bbox
        var listing1 = new Listing
        {
            FundaId = "5001",
            Address = "Address 5001",
            City = "CityOverlay",
            Latitude = 52.05,
            Longitude = 4.05,
            Price = 400000,
            LivingAreaM2 = 100,
            ListedDate = DateTime.UtcNow
        }; // 4000 / m2

        var listing2 = new Listing
        {
            FundaId = "5002",
            Address = "Address 5002",
            City = "CityOverlay",
            Latitude = 52.06,
            Longitude = 4.06,
            Price = 600000,
            LivingAreaM2 = 100,
            ListedDate = DateTime.UtcNow
        }; // 6000 / m2

        // Average should be 5000 / m2

        var listingOutside = new Listing
        {
            FundaId = "5003",
            Address = "Address 5003",
            City = "CityOverlay",
            Latitude = 53.0, // Outside
            Longitude = 5.0,
            Price = 1000000,
            LivingAreaM2 = 100,
            ListedDate = DateTime.UtcNow
        };

        var listingNoPrice = new Listing
        {
            FundaId = "5004",
            Address = "Address 5004",
            City = "CityOverlay",
            Latitude = 52.07,
            Longitude = 4.07,
            Price = null,
            LivingAreaM2 = 100,
            ListedDate = DateTime.UtcNow
        };

        DbContext.Listings.AddRange(listing1, listing2, listingOutside, listingNoPrice);
        await DbContext.SaveChangesAsync();

        await AuthenticateAsync();

        // Mock Geo Client to return a dummy overlay
        var dummyOverlay = new MapOverlayDto(
            Id: "BU001",
            Name: "Test Neighborhood",
            MetricName: "PricePerSquareMeter",
            MetricValue: 0, // Placeholder
            DisplayValue: "",
            GeoJson: JsonDocument.Parse("{}").RootElement); // Empty geometry for simplicity

        _fixture.Factory!.CbsGeoClientMock.Setup(x => x.GetNeighborhoodOverlaysAsync(
            minLat, minLon, maxLat, maxLon, MapOverlayMetric.PopulationDensity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayDto> { dummyOverlay });

        // Act
        var url = FormattableString.Invariant($"/api/map/overlays?minLat={minLat}&minLon={minLon}&maxLat={maxLat}&maxLon={maxLon}&metric=PricePerSquareMeter");
        var response = await Client.GetAsync(url);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<MapOverlayDto>>();
        Assert.NotNull(result);
        Assert.Single(result);

        var overlay = result.First();
        Assert.Equal("PricePerSquareMeter", overlay.MetricName);
        Assert.Equal(5000, overlay.MetricValue); // (4000 + 6000) / 2
        // Assuming localized formatting which might use comma or dot depending on culture.
        // We verify it contains "5" and "000".
        Assert.Contains("5", overlay.DisplayValue);
        Assert.Contains("000", overlay.DisplayValue);
    }
}
