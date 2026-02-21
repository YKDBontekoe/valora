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
using Xunit.Abstractions;
using System.Diagnostics;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class BatchJobBenchmarkTests : IAsyncLifetime
{
    private readonly TestcontainersDatabaseFixture _fixture;
    private readonly ITestOutputHelper _output;
    private BatchJobTestWebAppFactory _factory = null!;
    private readonly Mock<ICbsNeighborhoodStatsClient> _mockStatsClient = new();
    private readonly Mock<ICbsCrimeStatsClient> _mockCrimeClient = new();
    private readonly Mock<ICbsGeoClient> _mockGeoClient = new();

    public BatchJobBenchmarkTests(TestcontainersDatabaseFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
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
        private readonly BatchJobBenchmarkTests _testInstance;

        public BatchJobTestWebAppFactory(string connectionString, BatchJobBenchmarkTests testInstance)
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

                services.RemoveAll<ICbsGeoClient>();
                services.AddSingleton(_testInstance._mockGeoClient.Object);
            });
        }
    }

    [Fact]
    public async Task Benchmark_CityIngestion_Performance()
    {
        // Arrange
        int neighborhoodCount = 50;
        int apiDelayMs = 50;
        var city = "BenchmarkCity";

        var neighborhoods = Enumerable.Range(0, neighborhoodCount)
            .Select(i => new NeighborhoodGeometryDto($"BU{i:0000}", $"Neighborhood {i}", "Neighborhood", 52.0 + i * 0.001, 5.0 + i * 0.001))
            .ToList();

        _mockGeoClient
            .Setup(x => x.GetNeighborhoodsByMunicipalityAsync(city, It.IsAny<CancellationToken>()))
            .ReturnsAsync(neighborhoods);

        _mockStatsClient
            .Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .Returns(async (ResolvedLocationDto loc, CancellationToken token) =>
            {
                await Task.Delay(apiDelayMs, token);
                return new NeighborhoodStatsDto(
                    loc.NeighborhoodCode!, "Neighborhood", 1000, 5000, 450, 10, 500, 500, 150, 120, 300, 250, 180, 400, 350, 250, 2.1, "Urban", 35.0, 30.0, 20, 40, 40, 40, 60, 20, 40, 90, 10, 80, 0.5, 1000, 500, 0.5, 0.2, 0.4, 0.6, 5.0, DateTimeOffset.UtcNow);
            });

        _mockCrimeClient
            .Setup(x => x.GetStatsAsync(It.IsAny<ResolvedLocationDto>(), It.IsAny<CancellationToken>()))
            .Returns(async (ResolvedLocationDto loc, CancellationToken token) =>
            {
                await Task.Delay(apiDelayMs, token); // Simulate concurrent latency
                return new CrimeStatsDto(50, 5, 3, 20, 8, 5.2, DateTimeOffset.UtcNow);
            });

        using var scope = _factory.Services.CreateScope();
        var jobService = scope.ServiceProvider.GetRequiredService<IBatchJobService>();

        await jobService.EnqueueJobAsync(BatchJobType.CityIngestion, city);

        // Act
        var stopwatch = Stopwatch.StartNew();
        await jobService.ProcessNextJobAsync();
        stopwatch.Stop();

        _output.WriteLine($"Processed {neighborhoodCount} neighborhoods in {stopwatch.ElapsedMilliseconds}ms");

        // Assert
        using var assertScope = _factory.Services.CreateScope();
        var dbContext = assertScope.ServiceProvider.GetRequiredService<ValoraDbContext>();
        var count = await dbContext.Neighborhoods.CountAsync(n => n.City == city);
        Assert.Equal(neighborhoodCount, count);
    }
}
