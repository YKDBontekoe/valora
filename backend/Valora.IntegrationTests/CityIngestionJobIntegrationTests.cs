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
public class CityIngestionJobIntegrationTests : BaseTestcontainersIntegrationTest
{
    private readonly Mock<ICbsGeoClient> _mockGeoClient = new();
    private readonly Mock<ICbsNeighborhoodStatsClient> _mockStatsClient = new();
    private readonly Mock<ICbsCrimeStatsClient> _mockCrimeClient = new();

    public CityIngestionJobIntegrationTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    // Custom Factory to inject mocks
    private class CityIngestionWebAppFactory : IntegrationTestWebAppFactory
    {
        private readonly CityIngestionJobIntegrationTests _testInstance;

        public CityIngestionWebAppFactory(string connectionString, CityIngestionJobIntegrationTests testInstance)
            : base(connectionString)
        {
            _testInstance = testInstance;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICbsGeoClient>();
                services.AddSingleton(_testInstance._mockGeoClient.Object);

                services.RemoveAll<ICbsNeighborhoodStatsClient>();
                services.AddSingleton(_testInstance._mockStatsClient.Object);

                services.RemoveAll<ICbsCrimeStatsClient>();
                services.AddSingleton(_testInstance._mockCrimeClient.Object);
            });
        }
    }

    private string GetCurrentConnectionString()
    {
        if (DbContext.Database.ProviderName != null && DbContext.Database.ProviderName.Contains("InMemory"))
        {
            return "InMemory:TestcontainersFallback";
        }

        var connectionString = DbContext.Database.GetConnectionString();
        // If null, we are likely using InMemory database with the fallback name from TestcontainersDatabaseFixture
        return string.IsNullOrEmpty(connectionString) ? "InMemory:TestcontainersFallback" : connectionString;
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldIngestCityAndCompleteJob()
    {
        // Arrange
        await InitializeAsync();

        // 1. Setup Mocks
        var city = "TestCity";
        var neighborhoodCode = "NB01";

        _mockGeoClient
            .Setup(x => x.GetNeighborhoodsByMunicipalityAsync(city, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NeighborhoodGeometryDto>
            {
                new(neighborhoodCode, "Test Neighborhood", "Neighborhood", 52.0, 4.0)
            });

        _mockStatsClient
            .Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NeighborhoodStatsDto(
                neighborhoodCode, "Neighborhood", 1000, 2500, 300, 10, 500, 500,
                100, 100, 300, 300, 200, 400, 300, 300, 2.5, "Urban",
                30.0, 35.0, 20, 40, 40, 50, 50, 20, 30, 80, 20, 60,
                1.0, 500, 500, 1.0, 0.5, 0.5, 1.0, 3, DateTimeOffset.UtcNow));

        _mockCrimeClient
            .Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrimeStatsDto(50, 10, 5, 20, 15, 2.0, DateTimeOffset.UtcNow));

        // 2. Create Job in DB
        var job = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = city,
            Status = BatchJobStatus.Pending,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.BatchJobs.Add(job);
        await DbContext.SaveChangesAsync();

        // 3. Create Custom Factory Scope
        var connectionString = GetCurrentConnectionString();

        await using var factory = new CityIngestionWebAppFactory(connectionString, this);
        using var scope = factory.Services.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<IBatchJobExecutor>();

        // Act
        // ProcessNextJobAsync finds the next pending job and executes it.
        // It uses its own DbContext from the scope.
        await executor.ProcessNextJobAsync(CancellationToken.None);

        // Assert
        // Reload job from the test context (which shares the DB) to see updates
        await DbContext.Entry(job).ReloadAsync();

        Assert.Equal(BatchJobStatus.Completed, job.Status);
        Assert.Equal(100, job.Progress);
        Assert.NotNull(job.CompletedAt);
        Assert.Contains("Processed 1 neighborhoods", job.ResultSummary);
        Assert.Contains("Job completed successfully", job.ExecutionLog);

        // Verify Neighborhood Persistence
        // Since we are using shared DB, we can query using DbContext
        var neighborhood = await DbContext.Neighborhoods.FirstOrDefaultAsync(n => n.Code == neighborhoodCode);
        Assert.NotNull(neighborhood);
        Assert.Equal("Test Neighborhood", neighborhood.Name);
        Assert.Equal(city, neighborhood.City);
        Assert.Equal(52.0, neighborhood.Latitude);
        Assert.Equal(4.0, neighborhood.Longitude);
        Assert.Equal(300 * 1000, neighborhood.AverageWozValue); // 300k
        Assert.Equal(2500, neighborhood.PopulationDensity);
        Assert.Equal(50, neighborhood.CrimeRate);

        // Verify Mocks Called
        _mockGeoClient.Verify(x => x.GetNeighborhoodsByMunicipalityAsync(city, It.IsAny<CancellationToken>()), Times.Once);
        // The processor makes calls for each neighborhood found.
        _mockStatsClient.Verify(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCrimeClient.Verify(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldHandleProcessorFailure()
    {
        // Arrange
        await InitializeAsync();

        var city = "FailCity";

        // Mock failure
        _mockGeoClient
            .Setup(x => x.GetNeighborhoodsByMunicipalityAsync(city, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Geo API Down"));

        var job = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = city,
            Status = BatchJobStatus.Pending,
            Progress = 0,
            CreatedAt = DateTime.UtcNow
        };
        DbContext.BatchJobs.Add(job);
        await DbContext.SaveChangesAsync();

        var connectionString = GetCurrentConnectionString();
        await using var factory = new CityIngestionWebAppFactory(connectionString, this);
        using var scope = factory.Services.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<IBatchJobExecutor>();

        // Act
        await executor.ProcessNextJobAsync(CancellationToken.None);

        // Assert
        await DbContext.Entry(job).ReloadAsync();

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        // The executor catches the exception and logs it internally, setting a generic error message
        Assert.Equal("Job failed due to an internal error.", job.Error);
        Assert.NotNull(job.CompletedAt);

        // Verify Mocks
        _mockGeoClient.Verify(x => x.GetNeighborhoodsByMunicipalityAsync(city, It.IsAny<CancellationToken>()), Times.Once);
    }
}
