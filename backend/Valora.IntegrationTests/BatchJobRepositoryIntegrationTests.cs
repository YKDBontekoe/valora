using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;
using Shouldly;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class BatchJobRepositoryIntegrationTests : BaseTestcontainersIntegrationTest
{
    public BatchJobRepositoryIntegrationTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetNextPendingJobAsync_ShouldAtomicallyClaimJob()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBatchJobRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var oldestJob = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = "OldestCity",
            Status = BatchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-10)
        };

        var newerJob = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = "NewerCity",
            Status = BatchJobStatus.Pending,
            CreatedAt = DateTime.UtcNow.AddMinutes(-5)
        };

        var processingJob = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = "ProcessingCity",
            Status = BatchJobStatus.Processing,
            CreatedAt = DateTime.UtcNow.AddMinutes(-15)
        };

        dbContext.BatchJobs.AddRange(oldestJob, newerJob, processingJob);
        await dbContext.SaveChangesAsync();

        // Act
        var claimedJob = await repository.GetNextPendingJobAsync();

        // Assert
        claimedJob.ShouldNotBeNull();
        claimedJob.Id.ShouldBe(oldestJob.Id);
        claimedJob.Status.ShouldBe(BatchJobStatus.Processing);
        claimedJob.StartedAt.ShouldNotBeNull();

        // Verify side effects in DB
        var dbJob = await dbContext.BatchJobs.FindAsync(oldestJob.Id);
        dbJob!.Status.ShouldBe(BatchJobStatus.Processing);
        dbJob.StartedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetNextPendingJobAsync_ShouldReturnNull_WhenNoPendingJobs()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBatchJobRepository>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var processingJob = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = "ProcessingCity",
            Status = BatchJobStatus.Processing,
            CreatedAt = DateTime.UtcNow.AddMinutes(-15)
        };

        var completedJob = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Target = "CompletedCity",
            Status = BatchJobStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddMinutes(-20)
        };

        dbContext.BatchJobs.AddRange(processingJob, completedJob);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetNextPendingJobAsync();

        // Assert
        result.ShouldBeNull();
    }
}
