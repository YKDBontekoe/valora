using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shouldly;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Services;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class BatchJobExecutorTests : BaseTestcontainersIntegrationTest
{
    public BatchJobExecutorTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
        Factory.CbsGeoClientMock.Reset();
        Factory.CbsNeighborhoodStatsClientMock.Reset();
        Factory.CbsCrimeStatsClientMock.Reset();
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldExecuteJob_WhenPendingJobExists()
    {
        // Arrange
        var testCity = "TestCity_Exec";
        var neighborhoodCode = "BU0001";

        // 1. Setup Mocks (Before inserting job to avoid race condition)
        var geoMock = Factory.CbsGeoClientMock;
        geoMock.Setup(x => x.GetNeighborhoodsByMunicipalityAsync(testCity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NeighborhoodGeometryDto>
            {
                new(neighborhoodCode, "Test Neighborhood", "Buurt", 52.0, 4.0)
            });

        var statsMock = Factory.CbsNeighborhoodStatsClientMock;
        statsMock.Setup(x => x.GetStatsAsync(It.Is<ResolvedLocationDto>(l => l.NeighborhoodCode == neighborhoodCode), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NeighborhoodStatsDto(
                RegionCode: neighborhoodCode,
                RegionType: "Neighborhood",
                Residents: 3000,
                PopulationDensity: 1500,
                AverageWozValueKeur: 2500,
                LowIncomeHouseholdsPercent: 10.5,
                Men: 1400, Women: 1600,
                Age0To15: 500, Age15To25: 400, Age25To45: 1000, Age45To65: 800, Age65Plus: 300,
                SingleHouseholds: 800, HouseholdsWithoutChildren: 600, HouseholdsWithChildren: 600,
                AverageHouseholdSize: 2.3,
                Urbanity: "High",
                AverageIncomePerRecipient: 35.5,
                AverageIncomePerInhabitant: 45.2,
                EducationLow: 200, EducationMedium: 1000, EducationHigh: 800,
                PercentageOwnerOccupied: 60, PercentageRental: 40, PercentageSocialHousing: 20, PercentagePrivateRental: 20,
                PercentagePre2000: 50, PercentagePost2000: 50, PercentageMultiFamily: 40,
                CarsPerHousehold: 1.2, CarDensity: 100, TotalCars: 2000,
                DistanceToGp: 0.5, DistanceToSupermarket: 0.3, DistanceToDaycare: 0.4, DistanceToSchool: 0.6, SchoolsWithin3km: 5.0,
                RetrievedAtUtc: DateTimeOffset.UtcNow
            ));

        var crimeMock = Factory.CbsCrimeStatsClientMock;
        crimeMock.Setup(x => x.GetStatsAsync(It.Is<ResolvedLocationDto>(l => l.NeighborhoodCode == neighborhoodCode), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrimeStatsDto(
                TotalCrimesPer1000: 50,
                BurglaryPer1000: 10,
                ViolentCrimePer1000: 5,
                TheftPer1000: 20,
                VandalismPer1000: 15,
                YearOverYearChangePercent: -2.5,
                RetrievedAtUtc: DateTimeOffset.UtcNow
            ));

        // 2. Create Pending Job
        var job = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = testCity,
            Status = BatchJobStatus.Pending,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            context.BatchJobs.Add(job);
            await context.SaveChangesAsync();
        }

        // Act
        using (var scope = Factory.Services.CreateScope())
        {
            var executor = scope.ServiceProvider.GetRequiredService<IBatchJobExecutor>();
            await executor.ProcessNextJobAsync(CancellationToken.None);
        }

        // Assert
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

            // 1. Verify Job Status
            var processedJob = await context.BatchJobs.FindAsync(job.Id);
            processedJob.ShouldNotBeNull();
            // If the race condition happened, this will still be Completed.
            processedJob!.Status.ShouldBe(BatchJobStatus.Completed);
            processedJob.Progress.ShouldBe(100);
            processedJob.CompletedAt.ShouldNotBeNull();
            processedJob.ExecutionLog.ShouldNotBeNull();
            processedJob.ExecutionLog.ShouldContain("Job completed successfully.");

            // 2. Verify Side Effects (Neighborhood Created)
            var neighborhood = await context.Neighborhoods.FirstOrDefaultAsync(n => n.Code == neighborhoodCode);
            neighborhood.ShouldNotBeNull();
            neighborhood!.City.ShouldBe(testCity);
            neighborhood.PopulationDensity.ShouldBe(1500);
            neighborhood.AverageWozValue.ShouldBe(2500000); // 2500 * 1000
            neighborhood.CrimeRate.ShouldBe(50);
        }
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldHandleFailure_WhenProcessorThrows()
    {
        // Arrange
        var testCity = "FailCity";

        // 1. Setup Mock to Throw (Before inserting job)
        var geoMock = Factory.CbsGeoClientMock;
        geoMock.Setup(x => x.GetNeighborhoodsByMunicipalityAsync(testCity, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Simulated API Failure"));

        // 2. Create Pending Job
        var job = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = testCity,
            Status = BatchJobStatus.Pending,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            context.BatchJobs.Add(job);
            await context.SaveChangesAsync();
        }

        // Act
        using (var scope = Factory.Services.CreateScope())
        {
            var executor = scope.ServiceProvider.GetRequiredService<IBatchJobExecutor>();
            // Should not throw, but handle internally
            await executor.ProcessNextJobAsync(CancellationToken.None);
        }

        // Assert
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

            var failedJob = await context.BatchJobs.FindAsync(job.Id);
            failedJob.ShouldNotBeNull();
            failedJob!.Status.ShouldBe(BatchJobStatus.Failed);
            failedJob.Error.ShouldBe("Job failed due to an internal error."); // From BatchJobExecutor
            failedJob.ExecutionLog.ShouldNotBeNull();
            failedJob.ExecutionLog.ShouldContain("Job failed due to an internal error.");
        }
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldDoNothing_WhenNoPendingJobs()
    {
        // Arrange
        // Ensure only non-pending jobs exist
        var job = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = "CompletedCity",
            Status = BatchJobStatus.Completed,
            Progress = 100,
            CreatedAt = DateTime.UtcNow
        };

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            context.BatchJobs.Add(job);
            await context.SaveChangesAsync();
        }

        // Act
        using (var scope = Factory.Services.CreateScope())
        {
            var executor = scope.ServiceProvider.GetRequiredService<IBatchJobExecutor>();
            await executor.ProcessNextJobAsync(CancellationToken.None);
        }

        // Assert
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var existingJob = await context.BatchJobs.FindAsync(job.Id);

            // Should remain unchanged
            existingJob.ShouldNotBeNull();
            existingJob!.Status.ShouldBe(BatchJobStatus.Completed);
        }
    }
}
