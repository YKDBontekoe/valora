using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.DTOs.Map;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class BatchJobIntegrationTests : IAsyncLifetime
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private BatchJobTestWebAppFactory _factory = null!;
    private readonly Mock<ICbsNeighborhoodStatsClient> _mockStatsClient = new();
    private readonly Mock<ICbsCrimeStatsClient> _mockCrimeClient = new();

    public BatchJobIntegrationTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new BatchJobTestWebAppFactory(_fixture.ConnectionString, this);

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        // Cleanup existing data
        if (context.BatchJobs.Any())
        {
            context.BatchJobs.RemoveRange(context.BatchJobs);
        }
        if (context.Neighborhoods.Any())
        {
            context.Neighborhoods.RemoveRange(context.Neighborhoods);
        }
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory != null)
        {
            await _factory.DisposeAsync();
        }
    }

    private class BatchJobTestWebAppFactory : IntegrationTestWebAppFactory
    {
        private readonly BatchJobIntegrationTests _testInstance;

        public BatchJobTestWebAppFactory(string connectionString, BatchJobIntegrationTests testInstance)
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
                services.RemoveAll<ICbsNeighborhoodStatsClient>();
                services.AddSingleton(_testInstance._mockStatsClient.Object);

                services.RemoveAll<ICbsCrimeStatsClient>();
                services.AddSingleton(_testInstance._mockCrimeClient.Object);

                // Note: ICbsGeoClient is already mocked in the base factory,
                // but we can rely on that or override it if needed.
                // Since we need to access the mock to setup expectations, we can access it via the base factory property.
                // However, the base factory creates its own mock instance.
                // To coordinate, we can just use the base factory's CbsGeoClientMock.
            });
        }
    }

    [Fact]
    public async Task CityIngestion_UpdatesDatabase_Success()
    {
        // Arrange
        var city = "Utrecht";
        var neighborhoodCode = "BU0001";
        var neighborhoodName = "Center";

        // Setup GeoClient mock (inherited from base factory)
        _factory.CbsGeoClientMock
            .Setup(x => x.GetNeighborhoodsByMunicipalityAsync(city, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NeighborhoodGeometryDto>
            {
                new(neighborhoodCode, neighborhoodName, "Neighborhood", 52.0907, 5.1214)
            });

        // Setup StatsClient mock
        _mockStatsClient
            .Setup(x => x.GetStatsAsync(It.Is<ResolvedLocationDto>(l => l.NeighborhoodCode == neighborhoodCode), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NeighborhoodStatsDto(
                RegionCode: neighborhoodCode,
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

        // Setup CrimeClient mock
        _mockCrimeClient
            .Setup(x => x.GetStatsAsync(It.Is<ResolvedLocationDto>(l => l.NeighborhoodCode == neighborhoodCode), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrimeStatsDto(
                TotalCrimesPer1000: 50,
                BurglaryPer1000: 5,
                ViolentCrimePer1000: 3,
                TheftPer1000: 20,
                VandalismPer1000: 8,
                YearOverYearChangePercent: 5.2,
                RetrievedAtUtc: DateTimeOffset.UtcNow));

        using var scope = _factory.Services.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<IBatchJobService>();

        // Act
        var job = await jobService.EnqueueJobAsync(BatchJobType.CityIngestion, city);

        // Manually trigger processing
        await jobService.ProcessNextJobAsync();

        // Assert
        using var assertScope = _factory.Services.CreateScope();
        var dbContext = assertScope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        // Verify Job
        var updatedJob = await dbContext.BatchJobs.FindAsync(job.Id);
        Assert.NotNull(updatedJob);
        Assert.Equal(BatchJobStatus.Completed, updatedJob.Status);
        Assert.Equal(100, updatedJob.Progress);
        Assert.Contains("Processed 1 neighborhoods", updatedJob.ResultSummary);

        // Verify Neighborhood
        var neighborhood = await dbContext.Neighborhoods.FirstOrDefaultAsync(n => n.Code == neighborhoodCode);
        Assert.NotNull(neighborhood);
        Assert.Equal(neighborhoodName, neighborhood.Name);
        Assert.Equal(city, neighborhood.City);
        Assert.Equal(5000, neighborhood.PopulationDensity); // From mock
        Assert.Equal(450000, neighborhood.AverageWozValue); // 450 * 1000
        Assert.Equal(50, neighborhood.CrimeRate); // From mock
    }
}
