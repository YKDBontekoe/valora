using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Services;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services;

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
    public void AppendLog_ShouldCreateLog_WhenLogIsEmpty()
    {
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Test" };
        _stateManager.AppendLog(job, "First message");

        Assert.Contains("First message", job.ExecutionLog);
        Assert.DoesNotContain(Environment.NewLine, job.ExecutionLog); // Should be single line
    }

    [Fact]
    public void AppendLog_ShouldAppendLog_WhenLogIsNotEmpty()
    {
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Test", ExecutionLog = "[Old] Log" };
        _stateManager.AppendLog(job, "New message");

        Assert.Contains("[Old] Log", job.ExecutionLog);
        Assert.Contains("New message", job.ExecutionLog);
        Assert.Contains(Environment.NewLine, job.ExecutionLog);
    }

    [Fact]
    public async Task UpdateJobStatusAsync_ShouldSetStartedAt_WhenStatusIsProcessing()
    {
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Test", Status = BatchJobStatus.Pending };

        await _stateManager.UpdateJobStatusAsync(job, BatchJobStatus.Processing);

        Assert.Equal(BatchJobStatus.Processing, job.Status);
        Assert.NotNull(job.StartedAt);
        Assert.Contains("Job started.", job.ExecutionLog);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateJobStatusAsync_ShouldSetCompletedAtAndProgress_WhenStatusIsCompleted()
    {
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Test", Status = BatchJobStatus.Processing };

        await _stateManager.UpdateJobStatusAsync(job, BatchJobStatus.Completed);

        Assert.Equal(BatchJobStatus.Completed, job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.Equal(100, job.Progress);
        Assert.Contains("Job completed successfully.", job.ExecutionLog);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateJobStatusAsync_ShouldHandleFailure_WithException()
    {
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Test", Status = BatchJobStatus.Processing };
        var exception = new Exception("Critical failure");

        await _stateManager.UpdateJobStatusAsync(job, BatchJobStatus.Failed, ex: exception);

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.Equal("Job failed due to an internal error.", job.Error); // Sanitized error
        Assert.Contains("Job failed due to an internal error.", job.ExecutionLog);

        // Verify logging
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);

        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateJobStatusAsync_ShouldHandleFailure_WithoutException()
    {
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Test", Status = BatchJobStatus.Processing };
        var message = "Validation failed";

        await _stateManager.UpdateJobStatusAsync(job, BatchJobStatus.Failed, message: message);

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        Assert.NotNull(job.CompletedAt);
        Assert.Equal(message, job.Error);
        Assert.Contains(message, job.ExecutionLog);

        // Verify logging (Info level for non-exception failures)
        _loggerMock.Verify(
             x => x.Log(
                 LogLevel.Information,
                 It.IsAny<EventId>(),
                 It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(message)),
                 null,
                 It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
             Times.Once);

        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.Once);
    }
}
