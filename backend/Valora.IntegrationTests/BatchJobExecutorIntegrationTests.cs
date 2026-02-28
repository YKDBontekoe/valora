using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Shouldly;
using Valora.Application.Common.Events;
using Valora.Application.Common.Interfaces;
using Valora.Application.Services.BatchJobs;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class BatchJobExecutorIntegrationTests : BaseTestcontainersIntegrationTest
{
    public BatchJobExecutorIntegrationTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldProcessPendingJob_AndCompleteIt()
    {
        // Arrange
        var mockProcessor = new Mock<IBatchJobProcessor>();
        mockProcessor.SetupGet(p => p.JobType).Returns(BatchJobType.CityIngestion);
        mockProcessor.Setup(p => p.ProcessAsync(It.IsAny<BatchJob>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockEventDispatcher = new Mock<IEventDispatcher>();

        var testFactory = Factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IBatchJobProcessor>();
                services.AddScoped<IBatchJobProcessor>(_ => mockProcessor.Object);

                services.RemoveAll<IEventDispatcher>();
                services.AddScoped<IEventDispatcher>(_ => mockEventDispatcher.Object);
            });
        });

        using var scope = testFactory.Services.CreateScope();
        var executor = scope.ServiceProvider.GetRequiredService<IBatchJobExecutor>();
        var dbContext = scope.ServiceProvider.GetRequiredService<ValoraDbContext>();

        var job = new BatchJob
        {
            Type = BatchJobType.CityIngestion,
            Status = BatchJobStatus.Pending,
            Target = "TestCity",
            CreatedAt = DateTime.UtcNow
        };

        dbContext.BatchJobs.Add(job);
        await dbContext.SaveChangesAsync();

        // Act
        await executor.ProcessNextJobAsync();

        // Assert
        var updatedJob = await dbContext.BatchJobs.FindAsync(job.Id);
        updatedJob.ShouldNotBeNull();
        updatedJob!.Status.ShouldBe(BatchJobStatus.Completed);
        updatedJob.Progress.ShouldBe(100);
        updatedJob.CompletedAt.ShouldNotBeNull();

        mockProcessor.Verify(p => p.ProcessAsync(It.Is<BatchJob>(j => j.Id == job.Id), It.IsAny<CancellationToken>()), Times.Once);

        mockEventDispatcher.Verify(d => d.DispatchAsync(
            It.Is<IDomainEvent>(e => e is BatchJobCompletedEvent && ((BatchJobCompletedEvent)e).JobId == job.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
