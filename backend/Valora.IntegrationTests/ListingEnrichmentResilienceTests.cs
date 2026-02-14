using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class ListingEnrichmentResilienceTests : IAsyncLifetime
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private readonly Mock<ILocationResolver> _mockLocationResolver = new();
    private readonly Mock<ICbsNeighborhoodStatsClient> _mockCbsClient = new();
    private readonly Mock<ICbsCrimeStatsClient> _mockCrimeClient = new();
    private readonly Mock<IAmenityClient> _mockAmenityClient = new();
    private readonly Mock<IAirQualityClient> _mockAirQualityClient = new();

    private HttpClient _client = null!;
    private ResilienceTestWebAppFactory _factory = null!;

    public ListingEnrichmentResilienceTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Use the connection string from the fixture which points to the Testcontainers instance
        _factory = new ResilienceTestWebAppFactory(_fixture.ConnectionString, this);
        _client = _factory.CreateClient();

        // Ensure database is clean before each test
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        // Cleanup existing data
        context.Listings.RemoveRange(context.Listings);
        if (context.Users.Any())
        {
            context.Users.RemoveRange(context.Users);
        }
        await context.SaveChangesAsync();

        await AuthenticateAsync();
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    private async Task AuthenticateAsync()
    {
        var password = "Password123!";
        var email = "resilience@test.local";

        // Register user
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });

        // Manually assign Admin role directly in DB
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            var user = await userManager.FindByEmailAsync(email);
            if (user != null)
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });

        loginResponse.EnsureSuccessStatusCode();

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (auth?.Token != null)
        {
            _client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
        }
    }

    private class ResilienceTestWebAppFactory : IntegrationTestWebAppFactory
    {
        private readonly ListingEnrichmentResilienceTests _testInstance;

        public ResilienceTestWebAppFactory(string connectionString, ListingEnrichmentResilienceTests testInstance)
            : base(connectionString)
        {
            _testInstance = testInstance;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                // Register mocks
                services.RemoveAll<ILocationResolver>();
                services.AddSingleton(_testInstance._mockLocationResolver.Object);

                services.RemoveAll<ICbsNeighborhoodStatsClient>();
                services.AddSingleton(_testInstance._mockCbsClient.Object);

                services.RemoveAll<ICbsCrimeStatsClient>();
                services.AddSingleton(_testInstance._mockCrimeClient.Object);

                services.RemoveAll<IAmenityClient>();
                services.AddSingleton(_testInstance._mockAmenityClient.Object);

                services.RemoveAll<IAirQualityClient>();
                services.AddSingleton(_testInstance._mockAirQualityClient.Object);
            });
        }
    }

    [Fact]
    public async Task Enrich_WithPartialFailure_UpdatesDatabaseAndAddsWarnings()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var listing = new Listing
        {
            FundaId = "Resilience123",
            Address = "Resilience Street 1",
            City = "Utrecht",
            Price = 400000,
            ListedDate = DateTime.UtcNow
        };
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync();

        // Setup successful mocks
        var resolvedLocation = new ResolvedLocationDto(
            Query: "Resilience Street 1",
            DisplayAddress: "Resilience Street 1, 3500AA Utrecht",
            Latitude: 52.0907,
            Longitude: 5.1214,
            RdX: 136000,
            RdY: 456000,
            MunicipalityCode: "GM0344",
            MunicipalityName: "Utrecht",
            DistrictCode: "WK034400",
            DistrictName: "Binnenstad",
            NeighborhoodCode: "BU03440001",
            NeighborhoodName: "Lange Elizabethstraat en omgeving",
            PostalCode: "3500AA");

        _mockLocationResolver
            .Setup(x => x.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedLocation);

        _mockCbsClient
            .Setup(x => x.GetStatsAsync(resolvedLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NeighborhoodStatsDto(
                RegionCode: "BU03440001",
                RegionType: "Neighborhood",
                Residents: 2000,
                PopulationDensity: 8000,
                AverageWozValueKeur: 500,
                LowIncomeHouseholdsPercent: 15,
                Men: 1000,
                Women: 1000,
                Age0To15: 300,
                Age15To25: 400,
                Age25To45: 800,
                Age45To65: 300,
                Age65Plus: 200,
                SingleHouseholds: 800,
                HouseholdsWithoutChildren: 600,
                HouseholdsWithChildren: 400,
                AverageHouseholdSize: 1.8,
                Urbanity: "Zeer sterk stedelijk",
                AverageIncomePerRecipient: 40.0,
                AverageIncomePerInhabitant: 35.0,
                EducationLow: 10,
                EducationMedium: 30,
                EducationHigh: 60,
                PercentageOwnerOccupied: 30,
                PercentageRental: 70,
                PercentageSocialHousing: 30,
                PercentagePrivateRental: 40,
                PercentagePre2000: 95,
                PercentagePost2000: 5,
                PercentageMultiFamily: 90,
                CarsPerHousehold: 0.3,
                CarDensity: 1500,
                TotalCars: 600,
                DistanceToGp: 0.3,
                DistanceToSupermarket: 0.2,
                DistanceToDaycare: 0.4,
                DistanceToSchool: 0.5,
                SchoolsWithin3km: 10.0,
                RetrievedAtUtc: DateTimeOffset.UtcNow));

        _mockAmenityClient
            .Setup(x => x.GetAmenitiesAsync(resolvedLocation, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AmenityStatsDto(
                SchoolCount: 5,
                SupermarketCount: 8,
                ParkCount: 2,
                HealthcareCount: 6,
                TransitStopCount: 15,
                NearestAmenityDistanceMeters: 20,
                DiversityScore: 0.9,
                RetrievedAtUtc: DateTimeOffset.UtcNow));

        _mockAirQualityClient
            .Setup(x => x.GetSnapshotAsync(resolvedLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AirQualitySnapshotDto(
                StationId: "NL456",
                StationName: "Utrecht-Griftpark",
                StationDistanceMeters: 1000,
                Pm25: 10.0,
                MeasuredAtUtc: DateTimeOffset.UtcNow,
                RetrievedAtUtc: DateTimeOffset.UtcNow));

        // Setup failure for Crime Client
        _mockCrimeClient
            .Setup(x => x.GetStatsAsync(resolvedLocation, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("CBS Crime API unavailable"));

        // Act
        var response = await _client.PostAsync($"/api/listings/{listing.Id}/enrich", null);

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify DB update
        using var assertScope = _factory.Services.CreateScope();
        var assertDbContext = assertScope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var updatedListing = await assertDbContext.Listings.FindAsync(listing.Id);
        Assert.NotNull(updatedListing);

        // Assert scores
        Assert.NotNull(updatedListing.ContextCompositeScore);
        Assert.True(updatedListing.ContextCompositeScore > 0, "Composite score should be calculated despite missing safety data");

        Assert.NotNull(updatedListing.ContextSocialScore); // Should be present (from CBS)
        Assert.NotNull(updatedListing.ContextAmenitiesScore); // Should be present (from Amenities)
        Assert.NotNull(updatedListing.ContextEnvironmentScore); // Should be present (from AirQuality)

        // Safety Score should be null because the source failed
        Assert.Null(updatedListing.ContextSafetyScore);

        // Assert Report JSON contains warnings
        Assert.NotNull(updatedListing.ContextReport);
        Assert.NotNull(updatedListing.ContextReport.Warnings);
        Assert.NotEmpty(updatedListing.ContextReport.Warnings);

        // We expect two warnings: one from ContextDataProvider ("Source CBS Crime unavailable")
        // and one from CrimeMetricBuilder ("CBS crime statistics were unavailable...")
        Assert.Contains(updatedListing.ContextReport.Warnings, w => w.Contains("Source CBS Crime unavailable"));
        Assert.Contains(updatedListing.ContextReport.Warnings, w => w.Contains("CBS crime statistics were unavailable"));
    }
}
