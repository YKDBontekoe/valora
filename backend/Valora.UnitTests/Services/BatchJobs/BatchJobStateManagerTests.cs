using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Services.BatchJobs;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services.BatchJobs;

public class BatchJobStateManagerTests
{
    private readonly Mock<IBatchJobRepository> _jobRepositoryMock = new();
    private readonly Mock<ILogger<BatchJobStateManager>> _loggerMock = new();
    private readonly BatchJobStateManager _stateManager;

    public BatchJobStateManagerTests()
    {
        _stateManager = new BatchJobStateManager(_jobRepositoryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task MarkJobStartedAsync_ShouldUpdateStatusAndLog()
    {
        var job = new BatchJob { Status = BatchJobStatus.Pending, Type = BatchJobType.CityIngestion, Target = "Test" };

        await _stateManager.MarkJobStartedAsync(job);

        Assert.Equal(BatchJobStatus.Processing, job.Status);
        Assert.NotNull(job.StartedAt);
        Assert.Contains("Job started.", job.ExecutionLog);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkJobCompletedAsync_ShouldUpdateStatusAndLog()
    {
        var job = new BatchJob { Status = BatchJobStatus.Processing, StartedAt = DateTime.UtcNow.AddMinutes(-1), Type = BatchJobType.CityIngestion, Target = "Test" };

        await _stateManager.MarkJobCompletedAsync(job, "Success");

        Assert.Equal(BatchJobStatus.Completed, job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.Equal(100, job.Progress);
        Assert.Contains("Success", job.ExecutionLog);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkJobFailedAsync_WithMessage_ShouldUpdateStatusAndLog()
    {
        var job = new BatchJob { Status = BatchJobStatus.Processing, StartedAt = DateTime.UtcNow.AddMinutes(-1), Type = BatchJobType.CityIngestion, Target = "Test" };

        await _stateManager.MarkJobFailedAsync(job, "Failure reason");

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.Equal("Failure reason", job.Error);
        Assert.Contains("Failure reason", job.ExecutionLog);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task MarkJobFailedAsync_WithException_ShouldUpdateStatusAndLogGenericError()
    {
        var job = new BatchJob { Status = BatchJobStatus.Processing, StartedAt = DateTime.UtcNow.AddMinutes(-1), Type = BatchJobType.CityIngestion, Target = "Test" };
        var exception = new Exception("Internal error");

        await _stateManager.MarkJobFailedAsync(job, null, exception);

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.Equal("Job failed due to an internal error.", job.Error);
        Assert.Contains("Job failed due to an internal error.", job.ExecutionLog);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.Once);

        // Verify logger was called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
