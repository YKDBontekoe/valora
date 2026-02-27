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
            _processors,
            _stateManagerMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldDoNothing_WhenNoPendingJobs()
    {
        var executor = CreateExecutor();
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((BatchJob?)null);

        await executor.ProcessNextJobAsync();

        _stateManagerMock.Verify(x => x.MarkJobStartedAsync(It.IsAny<BatchJob>(), It.IsAny<CancellationToken>()), Times.Never);
        _cityIngestionProcessorMock.Verify(x => x.ProcessAsync(It.IsAny<BatchJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldCallCorrectProcessor_AndMarkCompleted()
    {
        var executor = CreateExecutor();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await executor.ProcessNextJobAsync();

        _stateManagerMock.Verify(x => x.MarkJobStartedAsync(job, It.IsAny<CancellationToken>()), Times.Once);
        _cityIngestionProcessorMock.Verify(x => x.ProcessAsync(job, It.IsAny<CancellationToken>()), Times.Once);
        _stateManagerMock.Verify(x => x.MarkJobCompletedAsync(job, "Job completed successfully.", It.IsAny<CancellationToken>()), Times.Once);
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

        _stateManagerMock.Verify(x => x.MarkJobStartedAsync(job, It.IsAny<CancellationToken>()), Times.Once);
        _stateManagerMock.Verify(x => x.MarkJobFailedAsync(job, null, exception, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldFail_WhenNoProcessorFound()
    {
        // Setup executor with empty processors list
        var executor = new BatchJobExecutor(_jobRepositoryMock.Object, new List<IBatchJobProcessor>(), _stateManagerMock.Object, _loggerMock.Object);
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await executor.ProcessNextJobAsync();

        _stateManagerMock.Verify(x => x.MarkJobStartedAsync(job, It.IsAny<CancellationToken>()), Times.Once);

        // Verify that MarkJobFailedAsync is called with an exception
        _stateManagerMock.Verify(x => x.MarkJobFailedAsync(job, null, It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()), Times.Once);
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
        var executor = new BatchJobExecutor(_jobRepositoryMock.Object, processors, _stateManagerMock.Object, _loggerMock.Object);

        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await executor.ProcessNextJobAsync();

         // SingleOrDefault throws InvalidOperationException when > 1 match
        _stateManagerMock.Verify(x => x.MarkJobFailedAsync(job, null, It.IsAny<InvalidOperationException>(), It.IsAny<CancellationToken>()), Times.Once);
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

        _stateManagerMock.Verify(x => x.MarkJobFailedAsync(job, "Job cancelled by user.", null, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldNotMarkCompleted_IfChangedByProcessor()
    {
        var executor = CreateExecutor();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _cityIngestionProcessorMock.Setup(x => x.ProcessAsync(job, It.IsAny<CancellationToken>()))
            .Callback<BatchJob, CancellationToken>((j, ct) =>
            {
                // Simulate processor marking job as Failed (or some other status) internally
                // Note: In real life, the processor would probably call StateManager too, or modify the object directly.
                // Here we simulate the object modification.
                j.Status = BatchJobStatus.Failed;
            })
            .Returns(Task.CompletedTask);

        await executor.ProcessNextJobAsync();

        // Should NOT call MarkJobCompletedAsync because status is not Processing anymore
        _stateManagerMock.Verify(x => x.MarkJobCompletedAsync(It.IsAny<BatchJob>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
