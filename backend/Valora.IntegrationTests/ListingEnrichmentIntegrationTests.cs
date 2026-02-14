using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
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
public class ListingEnrichmentIntegrationTests : IAsyncLifetime
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private readonly Mock<ILocationResolver> _mockLocationResolver = new();
    private readonly Mock<ICbsNeighborhoodStatsClient> _mockCbsClient = new();
    private readonly Mock<ICbsCrimeStatsClient> _mockCrimeClient = new();
    private readonly Mock<IAmenityClient> _mockAmenityClient = new();
    private readonly Mock<IAirQualityClient> _mockAirQualityClient = new();

    private HttpClient _client = null!;
    private EnrichmentTestWebAppFactory _factory = null!;

    public ListingEnrichmentIntegrationTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        // Use the connection string from the fixture which points to the Testcontainers instance
        _factory = new EnrichmentTestWebAppFactory(_fixture.ConnectionString, this);
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
        var email = "enrichment@test.local";

        // Register user
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });

        // Manually assign Admin role directly in DB because we don't have an endpoint for it yet
        // and initial seeding might not run in test environment or we cleared users.
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();

            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole("Admin"));
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
        Assert.NotNull(auth);
        Assert.False(string.IsNullOrWhiteSpace(auth.Token));

        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.Token);
    }

    private class EnrichmentTestWebAppFactory : IntegrationTestWebAppFactory
    {
        private readonly ListingEnrichmentIntegrationTests _testInstance;

        public EnrichmentTestWebAppFactory(string connectionString, ListingEnrichmentIntegrationTests testInstance)
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
    public async Task Enrich_Listing_UpdatesDatabase()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var listing = new Listing
        {
            FundaId = "12345678",
            Address = "Test Street 1",
            City = "Amsterdam",
            Price = 500000,
            ListedDate = DateTime.UtcNow
        };
        dbContext.Listings.Add(listing);
        await dbContext.SaveChangesAsync();

        // Setup mocks
        var resolvedLocation = new ResolvedLocationDto(
            Query: "Test Street 1",
            DisplayAddress: "Test Street 1, 1012LG Amsterdam",
            Latitude: 52.37714,
            Longitude: 4.89803,
            RdX: 121691,
            RdY: 487809,
            MunicipalityCode: "GM0363",
            MunicipalityName: "Amsterdam",
            DistrictCode: "WK0363AD",
            DistrictName: "Burgwallen-Nieuwe Zijde",
            NeighborhoodCode: "BU0363AD03",
            NeighborhoodName: "Nieuwendijk-Noord",
            PostalCode: "1012LG");

        _mockLocationResolver
            .Setup(x => x.ResolveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedLocation);

        _mockCbsClient
            .Setup(x => x.GetStatsAsync(resolvedLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NeighborhoodStatsDto(
                RegionCode: "BU0363AD03",
                RegionType: "Neighborhood",
                Residents: 1000,
                PopulationDensity: 5000,
                AverageWozValueKeur: 450,
                LowIncomeHouseholdsPercent: 10,
                Men: 500,
                Women: 500,
                Age0To15: 150,
                Age15To25: 120,
                Age25To45: 300,
                Age45To65: 250,
                Age65Plus: 180,
                SingleHouseholds: 400,
                HouseholdsWithoutChildren: 350,
                HouseholdsWithChildren: 250,
                AverageHouseholdSize: 2.1,
                Urbanity: "Zeer sterk stedelijk",
                AverageIncomePerRecipient: 35.0,
                AverageIncomePerInhabitant: 30.0,
                EducationLow: 20,
                EducationMedium: 40,
                EducationHigh: 40,
                PercentageOwnerOccupied: 40,
                PercentageRental: 60,
                PercentageSocialHousing: 20,
                PercentagePrivateRental: 40,
                PercentagePre2000: 90,
                PercentagePost2000: 10,
                PercentageMultiFamily: 80,
                CarsPerHousehold: 0.5,
                CarDensity: 1000,
                TotalCars: 500,
                DistanceToGp: 0.5,
                DistanceToSupermarket: 0.2,
                DistanceToDaycare: 0.4,
                DistanceToSchool: 0.6,
                SchoolsWithin3km: 5.0,
                RetrievedAtUtc: DateTimeOffset.UtcNow));

        _mockCrimeClient
            .Setup(x => x.GetStatsAsync(resolvedLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrimeStatsDto(
                TotalCrimesPer1000: 45,
                BurglaryPer1000: 5,
                ViolentCrimePer1000: 3,
                TheftPer1000: 20,
                VandalismPer1000: 8,
                YearOverYearChangePercent: 5.2,
                RetrievedAtUtc: DateTimeOffset.UtcNow));

        _mockAmenityClient
            .Setup(x => x.GetAmenitiesAsync(resolvedLocation, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AmenityStatsDto(
                SchoolCount: 2,
                SupermarketCount: 5,
                ParkCount: 3,
                HealthcareCount: 4,
                TransitStopCount: 10,
                NearestAmenityDistanceMeters: 50,
                DiversityScore: 0.8,
                RetrievedAtUtc: DateTimeOffset.UtcNow));

        _mockAirQualityClient
            .Setup(x => x.GetSnapshotAsync(resolvedLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AirQualitySnapshotDto(
                StationId: "NL123",
                StationName: "Amsterdam-Station",
                StationDistanceMeters: 500,
                Pm25: 12.5,
                MeasuredAtUtc: DateTimeOffset.UtcNow,
                RetrievedAtUtc: DateTimeOffset.UtcNow));

        // Act
        // The endpoint is /api/listings/{id}/enrich
        // Note: The service uses "Input: listing.Address" internally to build the report
        var response = await _client.PostAsync($"/api/listings/{listing.Id}/enrich", null);

        // Assert
        response.EnsureSuccessStatusCode();

        // Verify DB update
        // We need to use a new context/scope to ensure we read from DB
        using var assertScope = _factory.Services.CreateScope();
        var assertDbContext = assertScope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var updatedListing = await assertDbContext.Listings.FindAsync(listing.Id);
        Assert.NotNull(updatedListing);

        // Assert scores are populated
        Assert.NotNull(updatedListing.ContextCompositeScore);
        Assert.True(updatedListing.ContextCompositeScore > 0, "Composite score should be > 0");

        Assert.NotNull(updatedListing.ContextSocialScore);
        Assert.NotNull(updatedListing.ContextSafetyScore);
        Assert.NotNull(updatedListing.ContextAmenitiesScore);
        Assert.NotNull(updatedListing.ContextEnvironmentScore);

        // Assert Report JSON is stored
        Assert.NotNull(updatedListing.ContextReport);
        Assert.Equal("Amsterdam", updatedListing.ContextReport.Location.MunicipalityName);
        Assert.NotEmpty(updatedListing.ContextReport.SocialMetrics);
    }
}
