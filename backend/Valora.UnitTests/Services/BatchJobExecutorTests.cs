using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Services;
using Valora.Application.Services.BatchJobs;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services;

public class BatchJobExecutorTests
{
    private readonly Mock<IBatchJobRepository> _jobRepositoryMock = new();
    private readonly Mock<IBatchJobStateManager> _stateManagerMock = new();
    private readonly Mock<ILogger<BatchJobExecutor>> _loggerMock = new();
    private readonly Mock<IBatchJobProcessor> _cityIngestionProcessorMock = new();
    private readonly List<IBatchJobProcessor> _processors = new();

    public BatchJobExecutorTests()
    {
        _cityIngestionProcessorMock.Setup(x => x.JobType).Returns(BatchJobType.CityIngestion);
        _processors.Add(_cityIngestionProcessorMock.Object);
    }

    private BatchJobExecutor CreateExecutor()
    {
        return new BatchJobExecutor(
            _jobRepositoryMock.Object,
            _stateManagerMock.Object,
            _processors,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldDoNothing_WhenNoPendingJobs()
    {
        var executor = CreateExecutor();
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((BatchJob?)null);

        await executor.ProcessNextJobAsync();

        _jobRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<BatchJob>(), It.IsAny<CancellationToken>()), Times.Never);
        _cityIngestionProcessorMock.Verify(x => x.ProcessAsync(It.IsAny<BatchJob>(), It.IsAny<CancellationToken>()), Times.Never);
        _stateManagerMock.Verify(x => x.UpdateJobStatusAsync(It.IsAny<BatchJob>(), It.IsAny<BatchJobStatus>(), It.IsAny<string>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldCallCorrectProcessor()
    {
        var executor = CreateExecutor();
        // Updated test expectation: The job returned by GetNextPendingJobAsync is already Processing
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await executor.ProcessNextJobAsync();

        _cityIngestionProcessorMock.Verify(x => x.ProcessAsync(job, It.IsAny<CancellationToken>()), Times.Once);
        // Verify state manager was called to complete the job
        _stateManagerMock.Verify(x => x.UpdateJobStatusAsync(job, BatchJobStatus.Completed, "Job completed successfully.", null, It.IsAny<CancellationToken>()), Times.Once);
        // Verify initial log was appended
        _stateManagerMock.Verify(x => x.AppendLog(job, "Job started."), Times.Once);
        // Verify repository update for the initial log
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldHandleProcessorFailure()
    {
        var executor = CreateExecutor();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        var exception = new Exception("Processor Error");
        _cityIngestionProcessorMock.Setup(x => x.ProcessAsync(job, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        await executor.ProcessNextJobAsync();

        // Verify state manager was called to fail the job
        _stateManagerMock.Verify(x => x.UpdateJobStatusAsync(job, BatchJobStatus.Failed, null, exception, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldFail_WhenNoProcessorFound()
    {
        // Setup executor with empty processors list
        var executor = new BatchJobExecutor(_jobRepositoryMock.Object, _stateManagerMock.Object, new List<IBatchJobProcessor>(), _loggerMock.Object);
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await executor.ProcessNextJobAsync();

        // Verify state manager was called to fail the job with generic error (due to InvalidOperationException inside executor)
        _stateManagerMock.Verify(x => x.UpdateJobStatusAsync(job, BatchJobStatus.Failed, null, It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldFail_WhenMultipleProcessorsFound()
    {
        // Setup executor with duplicate processors
        var processor1 = new Mock<IBatchJobProcessor>();
        processor1.Setup(x => x.JobType).Returns(BatchJobType.CityIngestion);
        var processor2 = new Mock<IBatchJobProcessor>();
        processor2.Setup(x => x.JobType).Returns(BatchJobType.CityIngestion);

        var processors = new List<IBatchJobProcessor> { processor1.Object, processor2.Object };
        var executor = new BatchJobExecutor(_jobRepositoryMock.Object, _stateManagerMock.Object, processors, _loggerMock.Object);

        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await executor.ProcessNextJobAsync();

        // Verify state manager was called to fail the job
        _stateManagerMock.Verify(x => x.UpdateJobStatusAsync(job, BatchJobStatus.Failed, null, It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldHandleCancellation()
    {
        var executor = CreateExecutor();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _cityIngestionProcessorMock.Setup(x => x.ProcessAsync(job, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await executor.ProcessNextJobAsync();

        // Verify state manager was called to fail the job with cancellation message
        _stateManagerMock.Verify(x => x.UpdateJobStatusAsync(job, BatchJobStatus.Failed, "Job cancelled by user.", null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldNotOverwriteStatus_IfChangedByProcessor()
    {
        var executor = CreateExecutor();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _cityIngestionProcessorMock.Setup(x => x.ProcessAsync(job, It.IsAny<CancellationToken>()))
            .Callback<BatchJob, CancellationToken>((j, ct) =>
            {
                // Simulate processor marking job as Failed (or some other status) internally
                j.Status = BatchJobStatus.Failed;
            })
            .Returns(Task.CompletedTask);

        await executor.ProcessNextJobAsync();

        // Should NOT call UpdateJobStatusAsync to set it to Completed because status is no longer Processing
        _stateManagerMock.Verify(x => x.UpdateJobStatusAsync(job, BatchJobStatus.Completed, It.IsAny<string>(), null, It.IsAny<CancellationToken>()), Times.Never);
    }
}
