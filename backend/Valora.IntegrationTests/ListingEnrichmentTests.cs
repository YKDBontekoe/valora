using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

public class ListingEnrichmentTests : IClassFixture<TestcontainersDatabaseFixture>, IAsyncLifetime
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    // Mocks
    private readonly Mock<ILocationResolver> _locationResolverMock = new();
    private readonly Mock<ICbsNeighborhoodStatsClient> _cbsClientMock = new();
    private readonly Mock<ICbsCrimeStatsClient> _crimeClientMock = new();
    private readonly Mock<IDemographicsClient> _demographicsClientMock = new();
    private readonly Mock<IAmenityClient> _amenityClientMock = new();
    private readonly Mock<IAirQualityClient> _airQualityClientMock = new();

    public ListingEnrichmentTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Configure the factory to swap out services with our mocks
        // We MUST store this factory instance because it contains the scoped services (like DbContext)
        // that are consistent with the HttpClient it creates.
        _factory = _fixture.Factory!.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => _locationResolverMock.Object);
                services.AddScoped(_ => _cbsClientMock.Object);
                services.AddScoped(_ => _crimeClientMock.Object);
                services.AddScoped(_ => _demographicsClientMock.Object);
                services.AddScoped(_ => _amenityClientMock.Object);
                services.AddScoped(_ => _airQualityClientMock.Object);
            });
        });

        _client = _factory.CreateClient();

        // Ensure clean state before each test
        // We use _factory.Services to ensure we are targeting the same InMemory/Container DB as the client
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        // Similar to BaseIntegrationTest.InitializeAsync
        context.Listings.RemoveRange(context.Listings);
        context.Notifications.RemoveRange(context.Notifications);
        if (context.Users.Any())
        {
            context.Users.RemoveRange(context.Users);
        }
        await context.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    private async Task AuthenticateAsync(string email = "enrich@test.com")
    {
        // Use a strong password to satisfy Identity requirements
        const string password = "StrongPassword123!";

        // Create user via UserManager to ensure they exist in the DB
        // Using _factory.Services ensures we are in the same scope as the API
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser { UserName = email, Email = email };
                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }

        // Login via API to get the token
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginDto(email, password));
        loginResponse.EnsureSuccessStatusCode();

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authResponse);
        Assert.False(string.IsNullOrEmpty(authResponse.Token), "Auth token should not be null or empty");

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
    }

    [Fact]
    public async Task Enrich_Listing_WithValidData_UpdatesDatabase()
    {
        // Arrange
        await AuthenticateAsync();
        SetupMocksWithValidData();

        var listingId = Guid.NewGuid();

        // Seed listing using the SAME factory scope
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            context.Listings.Add(new Listing
            {
                Id = listingId,
                FundaId = "11111",
                Address = "Damrak 1",
                City = "Amsterdam",
                Price = 1000000,
                ListedDate = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsync($"/api/listings/{listingId}/enrich", null);

        // Assert
        response.EnsureSuccessStatusCode();

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var listing = await context.Listings.AsNoTracking().FirstOrDefaultAsync(l => l.Id == listingId);

            Assert.NotNull(listing);
            Assert.NotNull(listing.ContextReport);
            Assert.NotNull(listing.ContextCompositeScore);
            Assert.True(listing.ContextCompositeScore > 50, "Score should be reasonably high with valid data");

            // Validate specific metric from mock
            var densityMetric = listing.ContextReport.SocialMetrics.FirstOrDefault(m => m.Key == "population_density");
            Assert.NotNull(densityMetric);
            Assert.Equal(3500, densityMetric.Value);

            // Validate safety score (mapped from Crime score)
            Assert.NotNull(listing.ContextSafetyScore);
            Assert.True(listing.ContextSafetyScore > 50);
        }
    }

    [Fact]
    public async Task Enrich_Listing_WhenPartialFailure_UpdatesWithWarnings()
    {
        // Arrange
        await AuthenticateAsync("enrich_partial@test.com");
        SetupMocksWithValidData();

        // Mock failure for AirQuality
        _airQualityClientMock.Setup(x => x.GetSnapshotAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API Down"));

        var listingId = Guid.NewGuid();
        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            context.Listings.Add(new Listing
            {
                Id = listingId,
                FundaId = "22222",
                Address = "Damrak 2",
                City = "Amsterdam",
                Price = 1000000,
                ListedDate = DateTime.UtcNow
            });
            await context.SaveChangesAsync();
        }

        // Act
        var response = await _client.PostAsync($"/api/listings/{listingId}/enrich", null);

        // Assert
        response.EnsureSuccessStatusCode(); // Should still be 200 OK because partial success is allowed

        using (var scope = _factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var listing = await context.Listings.AsNoTracking().FirstOrDefaultAsync(l => l.Id == listingId);

            Assert.NotNull(listing);
            Assert.NotNull(listing.ContextReport);

            // Verify warning
            Assert.Contains(listing.ContextReport.Warnings, w => w.Contains("Air quality source was unavailable"));

            // Verify partial metrics
            Assert.Empty(listing.ContextReport.EnvironmentMetrics); // Should be empty due to failure
            Assert.NotEmpty(listing.ContextReport.SocialMetrics); // Should be present
        }
    }

    private void SetupMocksWithValidData()
    {
        var location = new ResolvedLocationDto(
            Query: "Damrak 1",
            DisplayAddress: "Damrak 1, Amsterdam",
            Latitude: 52.3,
            Longitude: 4.9,
            RdX: null, RdY: null, MunicipalityCode: null, MunicipalityName: "Amsterdam",
            DistrictCode: null, DistrictName: null, NeighborhoodCode: null, NeighborhoodName: null, PostalCode: "1012AB");

        _locationResolverMock.Setup(x => x.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        _cbsClientMock.Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NeighborhoodStatsDto(
                RegionCode: "BU0000",
                RegionType: "Neighborhood",
                Residents: 1000,
                PopulationDensity: 3500,
                AverageWozValueKeur: 400,
                LowIncomeHouseholdsPercent: 10,
                RetrievedAtUtc: DateTimeOffset.UtcNow));

        _crimeClientMock.Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrimeStatsDto(
                TotalCrimesPer1000: 20,
                BurglaryPer1000: 2,
                ViolentCrimePer1000: 2,
                TheftPer1000: 5,
                VandalismPer1000: 5,
                YearOverYearChangePercent: 0,
                RetrievedAtUtc: DateTimeOffset.UtcNow));

        _demographicsClientMock.Setup(x => x.GetDemographicsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DemographicsDto(
                PercentAge0To14: 15,
                PercentAge15To24: 10,
                PercentAge25To44: 30,
                PercentAge45To64: 30,
                PercentAge65Plus: 15,
                AverageHouseholdSize: 2.5,
                PercentOwnerOccupied: 60,
                PercentSingleHouseholds: 40,
                PercentFamilyHouseholds: 25,
                RetrievedAtUtc: DateTimeOffset.UtcNow));

        _amenityClientMock.Setup(x => x.GetAmenitiesAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AmenityStatsDto(
                SchoolCount: 5,
                SupermarketCount: 2,
                ParkCount: 2,
                HealthcareCount: 3,
                TransitStopCount: 5,
                NearestAmenityDistanceMeters: 200,
                DiversityScore: 80,
                RetrievedAtUtc: DateTimeOffset.UtcNow));

        _airQualityClientMock.Setup(x => x.GetSnapshotAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AirQualitySnapshotDto(
                StationId: "1",
                StationName: "Station A",
                StationDistanceMeters: 500,
                Pm25: 8.5,
                MeasuredAtUtc: DateTimeOffset.UtcNow,
                RetrievedAtUtc: DateTimeOffset.UtcNow));
    }
}
