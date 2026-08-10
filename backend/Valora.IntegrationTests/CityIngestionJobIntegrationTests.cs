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
public class CityIngestionJobIntegrationTests : IClassFixture<TestcontainersDatabaseFixture>
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private readonly Mock<ICbsGeoClient> _mockGeoClient = new();
    private readonly Mock<ICbsNeighborhoodStatsClient> _mockStatsClient = new();
    private readonly Mock<ICbsCrimeStatsClient> _mockCrimeClient = new();

    public CityIngestionJobIntegrationTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
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
        // Use the connection string from the fixture's factory, or fallback if using InMemory.
        // We need to inspect the fixture's factory to see what it's using.
        // But we can't easily inspect the private fields of the fixture's factory.

        // However, we can create a temporary scope from the fixture to check the DB provider.
        // If the fixture failed to init Testcontainers, it uses "InMemory:TestcontainersFallback".
        if (_fixture.Factory == null) return "InMemory:TestcontainersFallback";

        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        if (context.Database.ProviderName != null && context.Database.ProviderName.Contains("InMemory"))
        {
            return "InMemory:CityIngestionJobTest"; // Use a unique name for this test class to avoid collisions
        }

        var connectionString = context.Database.GetConnectionString();
        return string.IsNullOrEmpty(connectionString) ? "InMemory:CityIngestionJobTest" : connectionString;
    }

    private async Task InitializeAsync(ValoraDbContext context)
    {
        // Manual cleanup since we are not using BaseTestcontainersIntegrationTest's DbContext
        context.BatchJobs.RemoveRange(context.BatchJobs);
        context.Neighborhoods.RemoveRange(context.Neighborhoods);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldIngestCityAndCompleteJob()
    {
        // Arrange
        var connectionString = GetCurrentConnectionString();
        await using var factory = new CityIngestionWebAppFactory(connectionString, this);

        // 1. Setup Data using the Factory's Context
        var city = "TestCity";
        var neighborhoodCode = "NB01";
        var jobId = Guid.NewGuid();

        using (var setupScope = factory.Services.CreateScope())
        {
            var setupContext = setupScope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            await InitializeAsync(setupContext);

            var job = new BatchJob
            {
                Id = jobId,
                Type = BatchJobType.CityIngestion,
                Target = city,
                Status = BatchJobStatus.Pending,
                Progress = 0,
                CreatedAt = DateTime.UtcNow
            };
            setupContext.BatchJobs.Add(job);
            await setupContext.SaveChangesAsync();
        }

        // 2. Setup Mocks
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

        // Act
        using (var actScope = factory.Services.CreateScope())
        {
            var executor = actScope.ServiceProvider.GetRequiredService<IBatchJobExecutor>();
            await executor.ProcessNextJobAsync(CancellationToken.None);
        }

        // Assert
        using (var assertScope = factory.Services.CreateScope())
        {
            var assertContext = assertScope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var job = await assertContext.BatchJobs.FindAsync(jobId);

            Assert.NotNull(job);
            Assert.Equal(BatchJobStatus.Completed, job.Status);
            Assert.Equal(100, job.Progress);
            Assert.NotNull(job.CompletedAt);
            Assert.Contains("Processed 1 neighborhoods", job.ResultSummary);
            Assert.Contains("Job completed successfully", job.ExecutionLog);

            // Verify Neighborhood Persistence
            var neighborhood = await assertContext.Neighborhoods.FirstOrDefaultAsync(n => n.Code == neighborhoodCode);
            Assert.NotNull(neighborhood);
            Assert.Equal("Test Neighborhood", neighborhood.Name);
            Assert.Equal(city, neighborhood.City);
            Assert.Equal(52.0, neighborhood.Latitude);
            Assert.Equal(4.0, neighborhood.Longitude);
            Assert.Equal(300 * 1000, neighborhood.AverageWozValue); // 300k
            Assert.Equal(2500, neighborhood.PopulationDensity);
            Assert.Equal(50, neighborhood.CrimeRate);
        }

        // Verify Mocks Called
        _mockGeoClient.Verify(x => x.GetNeighborhoodsByMunicipalityAsync(city, It.IsAny<CancellationToken>()), Times.Once);
        _mockStatsClient.Verify(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockCrimeClient.Verify(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldHandleProcessorFailure()
    {
        // Arrange
        var connectionString = GetCurrentConnectionString();
        await using var factory = new CityIngestionWebAppFactory(connectionString, this);

        var city = "FailCity";
        var jobId = Guid.NewGuid();

        using (var setupScope = factory.Services.CreateScope())
        {
            var setupContext = setupScope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            await InitializeAsync(setupContext);

            var job = new BatchJob
            {
                Id = jobId,
                Type = BatchJobType.CityIngestion,
                Target = city,
                Status = BatchJobStatus.Pending,
                Progress = 0,
                CreatedAt = DateTime.UtcNow
            };
            setupContext.BatchJobs.Add(job);
            await setupContext.SaveChangesAsync();
        }

        // Mock failure
        _mockGeoClient
            .Setup(x => x.GetNeighborhoodsByMunicipalityAsync(city, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Geo API Down"));

        // Act
        using (var actScope = factory.Services.CreateScope())
        {
            var executor = actScope.ServiceProvider.GetRequiredService<IBatchJobExecutor>();
            await executor.ProcessNextJobAsync(CancellationToken.None);
        }

        // Assert
        using (var assertScope = factory.Services.CreateScope())
        {
            var assertContext = assertScope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var job = await assertContext.BatchJobs.FindAsync(jobId);

            Assert.NotNull(job);
            Assert.Equal(BatchJobStatus.Failed, job.Status);
            Assert.Equal("Job failed due to an internal error.", job.Error);
            Assert.NotNull(job.CompletedAt);
        }

        // Verify Mocks
        _mockGeoClient.Verify(x => x.GetNeighborhoodsByMunicipalityAsync(city, It.IsAny<CancellationToken>()), Times.Once);
    }
}
