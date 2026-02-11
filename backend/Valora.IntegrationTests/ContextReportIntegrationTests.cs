using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Xunit;

namespace Valora.IntegrationTests;

public class ContextReportIntegrationTests : IClassFixture<TestcontainersDatabaseFixture>
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private readonly Mock<ILocationResolver> _mockLocationResolver = new();
    private readonly Mock<ICbsNeighborhoodStatsClient> _mockCbsClient = new();
    private readonly Mock<ICbsCrimeStatsClient> _mockCrimeClient = new();
    private readonly Mock<IDemographicsClient> _mockDemographicsClient = new();
    private readonly Mock<IAmenityClient> _mockAmenityClient = new();
    private readonly Mock<IAirQualityClient> _mockAirQualityClient = new();

    public ContextReportIntegrationTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    // Use a custom factory for this test class to inject mocks
    private class ContextReportTestWebAppFactory : IntegrationTestWebAppFactory
    {
        private readonly ContextReportIntegrationTests _testInstance;

        public ContextReportTestWebAppFactory(string connectionString, ContextReportIntegrationTests testInstance)
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

                services.RemoveAll<IDemographicsClient>();
                services.AddSingleton(_testInstance._mockDemographicsClient.Object);

                services.RemoveAll<IAmenityClient>();
                services.AddSingleton(_testInstance._mockAmenityClient.Object);

                services.RemoveAll<IAirQualityClient>();
                services.AddSingleton(_testInstance._mockAirQualityClient.Object);
            });
        }
    }

    private void SetupSuccessMocks(ResolvedLocationDto resolvedLocation)
    {
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
                Age0To15: 100,
                Age15To25: 150,
                Age25To45: 400,
                Age45To65: 250,
                Age65Plus: 100,
                SingleHouseholds: 60,
                HouseholdsWithoutChildren: 20,
                HouseholdsWithChildren: 20,
                AverageHouseholdSize: 1.8,
                Urbanity: "Zeer sterk stedelijk",
                AverageIncomePerRecipient: 30,
                AverageIncomePerInhabitant: 35,
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
                TotalCrimesPer1000: 50,
                BurglaryPer1000: 5,
                ViolentCrimePer1000: 2,
                TheftPer1000: 30,
                VandalismPer1000: 10,
                YearOverYearChangePercent: -2.5,
                RetrievedAtUtc: DateTimeOffset.UtcNow));

        _mockDemographicsClient
            .Setup(x => x.GetDemographicsAsync(resolvedLocation, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DemographicsDto(
                PercentAge0To14: 10,
                PercentAge15To24: 15,
                PercentAge25To44: 40,
                PercentAge45To64: 25,
                PercentAge65Plus: 10,
                AverageHouseholdSize: 1.8,
                PercentOwnerOccupied: 40,
                PercentSingleHouseholds: 60,
                PercentFamilyHouseholds: 40,
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
    }

    [Fact]
    public async Task BuildAsync_ReturnsCompleteReport_WhenAllSourcesSucceed()
    {
        // Arrange
        await using var factory = new ContextReportTestWebAppFactory(_fixture.ConnectionString, this);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IContextReportService>();

        var input = "Damrak 1 Amsterdam";
        var resolvedLocation = new ResolvedLocationDto(
            Query: input,
            DisplayAddress: "Damrak 1, 1012LG Amsterdam",
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
            .Setup(x => x.ResolveAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedLocation);

        SetupSuccessMocks(resolvedLocation);

        var request = new ContextReportRequestDto(Input: input, RadiusMeters: 500);

        // Act
        var report = await service.BuildAsync(request);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(resolvedLocation, report.Location);
        Assert.True(report.CompositeScore > 0, "Composite score should be calculated");
        Assert.True(report.CategoryScores.ContainsKey("Social"));
        Assert.True(report.CategoryScores.ContainsKey("Safety"));
        Assert.Empty(report.Warnings);
    }

    [Fact]
    public async Task BuildAsync_CachesResult_WhenSuccessful()
    {
        // Arrange
        await using var factory = new ContextReportTestWebAppFactory(_fixture.ConnectionString, this);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IContextReportService>();
        var cache = scope.ServiceProvider.GetRequiredService<IMemoryCache>();

        var input = "Museumplein 1 Amsterdam";
        var resolvedLocation = new ResolvedLocationDto(
            Query: input,
            DisplayAddress: "Museumplein 1, 1071XX Amsterdam",
            Latitude: 52.35825,
            Longitude: 4.88147,
            RdX: 120500,
            RdY: 486000,
            MunicipalityCode: "GM0363",
            MunicipalityName: "Amsterdam",
            DistrictCode: "WK0363AD",
            DistrictName: "Zuid",
            NeighborhoodCode: "BU0363AD08",
            NeighborhoodName: "Museumkwartier",
            PostalCode: "1071XX");

        _mockLocationResolver
            .Setup(x => x.ResolveAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedLocation);

        SetupSuccessMocks(resolvedLocation);

        var request = new ContextReportRequestDto(Input: input, RadiusMeters: 500);

        // Act
        await service.BuildAsync(request);

        // Assert
        // Cache Key format: context-report:v3:{lat_f5}_{lon_f5}:{radius}
        var latKey = resolvedLocation.Latitude.ToString("F5");
        var lonKey = resolvedLocation.Longitude.ToString("F5");
        var cacheKey = $"context-report:v3:{latKey}_{lonKey}:{request.RadiusMeters}";

        Assert.True(cache.TryGetValue(cacheKey, out ContextReportDto? cachedReport));
        Assert.NotNull(cachedReport);
        Assert.Equal(resolvedLocation.DisplayAddress, cachedReport!.Location.DisplayAddress);
    }

    [Fact]
    public async Task BuildAsync_ReturnsPartialReport_WhenNonCriticalSourceFails()
    {
        // Arrange
        await using var factory = new ContextReportTestWebAppFactory(_fixture.ConnectionString, this);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IContextReportService>();

        var input = "Kalverstraat 1 Amsterdam";
        var resolvedLocation = new ResolvedLocationDto(
            Query: input,
            DisplayAddress: "Kalverstraat 1, 1012NX Amsterdam",
            Latitude: 52.37021,
            Longitude: 4.89123,
            RdX: 121500,
            RdY: 487500,
            MunicipalityCode: "GM0363",
            MunicipalityName: "Amsterdam",
            DistrictCode: "WK0363AD",
            DistrictName: "Burgwallen-Nieuwe Zijde",
            NeighborhoodCode: "BU0363AD03",
            NeighborhoodName: "Nieuwendijk-Noord",
            PostalCode: "1012NX");

        _mockLocationResolver
            .Setup(x => x.ResolveAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolvedLocation);

        // Setup success for CBS
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
                Age0To15: 100,
                Age15To25: 150,
                Age25To45: 400,
                Age45To65: 250,
                Age65Plus: 100,
                SingleHouseholds: 60,
                HouseholdsWithoutChildren: 20,
                HouseholdsWithChildren: 20,
                AverageHouseholdSize: 1.8,
                Urbanity: "Zeer sterk stedelijk",
                AverageIncomePerRecipient: 30,
                AverageIncomePerInhabitant: 35,
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

        // Setup FAILURE for Air Quality
        _mockAirQualityClient
            .Setup(x => x.GetSnapshotAsync(resolvedLocation, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("API unavailable"));

        // Setup others as null/default or simple returns just to not break null checks if logic is robust
        _mockCrimeClient.Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>())).ReturnsAsync((CrimeStatsDto?)null);
        _mockDemographicsClient.Setup(x => x.GetDemographicsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>())).ReturnsAsync((DemographicsDto?)null);
        _mockAmenityClient.Setup(x => x.GetAmenitiesAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync((AmenityStatsDto?)null);

        var request = new ContextReportRequestDto(Input: input, RadiusMeters: 500);

        // Act
        var report = await service.BuildAsync(request);

        // Assert
        Assert.NotNull(report);
        // Environment category should be missing or empty because AirQuality failed
        Assert.False(report.CategoryScores.ContainsKey("Environment"), "Environment score should be missing due to failure");

        // Warnings should contain the failure
        Assert.Contains(report.Warnings, w => w.Contains("Air quality source was unavailable"));
        Assert.NotEmpty(report.SocialMetrics); // Should still have social metrics
    }

    [Fact]
    public async Task BuildAsync_ThrowsValidationException_WhenLocationResolverFails()
    {
        // Arrange
        await using var factory = new ContextReportTestWebAppFactory(_fixture.ConnectionString, this);
        using var scope = factory.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IContextReportService>();

        var input = "Unknown Place";

        _mockLocationResolver
            .Setup(x => x.ResolveAsync(input, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResolvedLocationDto?)null); // Resolver returns null if not found

        var request = new ContextReportRequestDto(Input: input, RadiusMeters: 500);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => service.BuildAsync(request));
    }
}
