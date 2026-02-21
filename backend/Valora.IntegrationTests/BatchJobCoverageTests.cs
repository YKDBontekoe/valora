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
public class BatchJobCoverageTests : IAsyncLifetime
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private BatchJobTestWebAppFactory _factory = null!;
    private readonly Mock<ICbsNeighborhoodStatsClient> _mockStatsClient = new();
    private readonly Mock<ICbsCrimeStatsClient> _mockCrimeClient = new();

    public BatchJobCoverageTests(TestcontainersDatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        _factory = new BatchJobTestWebAppFactory(_fixture.ConnectionString, this);
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        // Cleanup existing data
        if (context.BatchJobs.Any()) context.BatchJobs.RemoveRange(context.BatchJobs);
        if (context.Neighborhoods.Any()) context.Neighborhoods.RemoveRange(context.Neighborhoods);
        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync()
    {
        if (_factory != null) await _factory.DisposeAsync();
    }

    private class BatchJobTestWebAppFactory : IntegrationTestWebAppFactory
    {
        private readonly BatchJobCoverageTests _testInstance;

        public BatchJobTestWebAppFactory(string connectionString, BatchJobCoverageTests testInstance)
            : base(connectionString)
        {
            _testInstance = testInstance;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICbsNeighborhoodStatsClient>();
                services.AddSingleton(_testInstance._mockStatsClient.Object);
                services.RemoveAll<ICbsCrimeStatsClient>();
                services.AddSingleton(_testInstance._mockCrimeClient.Object);
            });
        }
    }

    [Fact]
    public async Task CityIngestion_NoNeighborhoods_ReturnsEarly()
    {
        // Arrange
        var city = "GhostTown";
        _factory.CbsGeoClientMock
            .Setup(x => x.GetNeighborhoodsByMunicipalityAsync(city, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<NeighborhoodGeometryDto>());

        using var scope = _factory.Services.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<IBatchJobService>();
        var job = await jobService.EnqueueJobAsync(BatchJobType.CityIngestion, city);

        // Act
        await jobService.ProcessNextJobAsync();

        // Assert
        using var assertScope = _factory.Services.CreateScope();
        var dbContext = assertScope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var updatedJob = await dbContext.BatchJobs.FindAsync(job.Id);

        Assert.NotNull(updatedJob);
        Assert.Equal(BatchJobStatus.Completed, updatedJob.Status);
        Assert.Equal("No neighborhoods found for city.", updatedJob.ResultSummary);
    }

    [Fact]
    public async Task CityIngestion_HandlesMixedAddAndUpdate_AndBatchesCorrectly()
    {
        // Arrange
        var city = "MixedCity";
        int totalItems = 15; // 5 existing + 10 new, forcing 2 batches (batch size 10)

        // Setup 5 existing neighborhoods in DB
        using (var seedScope = _factory.Services.CreateScope())
        {
            var seedContext = seedScope.ServiceProvider.GetRequiredService<ValoraDbContext>();
            var existingNeighborhoods = Enumerable.Range(0, 5).Select(i => new Neighborhood
            {
                Code = $"BU{i:0000}",
                Name = $"OldName {i}",
                City = city,
                Type = "Neighborhood",
                Latitude = 52.0 + i,
                Longitude = 5.0 + i,
                LastUpdated = DateTime.UtcNow.AddDays(-10) // Old date
            }).ToList();
            seedContext.Neighborhoods.AddRange(existingNeighborhoods);
            await seedContext.SaveChangesAsync();
        }

        // Mock API returning 15 items (0-4 match existing codes but have new data, 5-14 are new)
        var apiNeighborhoods = Enumerable.Range(0, totalItems).Select(i => new NeighborhoodGeometryDto(
            $"BU{i:0000}",
            $"NewName {i}", // Name update for existing (should be ignored for existing)
            "Neighborhood",
            52.0 + i,
            5.0 + i
        )).ToList();

        _factory.CbsGeoClientMock
            .Setup(x => x.GetNeighborhoodsByMunicipalityAsync(city, It.IsAny<CancellationToken>()))
            .ReturnsAsync(apiNeighborhoods);

        // Mock Stats/Crime clients
        _mockStatsClient
            .Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ResolvedLocationDto loc, CancellationToken token) => new NeighborhoodStatsDto(
                loc.NeighborhoodCode!, "Neighborhood", 1000,
                5000 + int.Parse(loc.NeighborhoodCode!.Substring(2)), // Unique value per item
                450, 10, 500, 500, 150, 120, 300, 250, 180, 400, 350, 250, 2.1, "Urban", 35.0, 30.0, 20, 40, 40, 40, 60, 20, 40, 90, 10, 80, 0.5, 1000, 500, 0.5, 0.2, 0.4, 0.6, 5.0, DateTimeOffset.UtcNow));

        _mockCrimeClient
            .Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CrimeStatsDto(50, 5, 3, 20, 8, 5.2, DateTimeOffset.UtcNow));

        using var scope = _factory.Services.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<IBatchJobService>();
        var job = await jobService.EnqueueJobAsync(BatchJobType.CityIngestion, city);

        // Act
        await jobService.ProcessNextJobAsync();

        // Assert
        using var assertScope = _factory.Services.CreateScope();
        var assertContext = assertScope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        // Verify Job
        var updatedJob = await assertContext.BatchJobs.FindAsync(job.Id);
        Assert.Equal(BatchJobStatus.Completed, updatedJob!.Status);
        Assert.Equal($"Processed {totalItems} neighborhoods.", updatedJob.ResultSummary);

        // Verify all 15 neighborhoods exist
        var allNeighborhoods = await assertContext.Neighborhoods.Where(n => n.City == city).OrderBy(n => n.Code).ToListAsync();
        Assert.Equal(totalItems, allNeighborhoods.Count);

        // Verify update happened for existing (check unique value)
        var first = allNeighborhoods.First(n => n.Code == "BU0000");
        Assert.Equal(5000, first.PopulationDensity); // 5000 + 0

        // Name should NOT be updated for existing entries logic
        Assert.Equal("OldName 0", first.Name);

        // Verify new item
        var last = allNeighborhoods.Last(n => n.Code == "BU0014");
        Assert.Equal(5014, last.PopulationDensity); // 5000 + 14
        Assert.Equal("NewName 14", last.Name);
    }
}
