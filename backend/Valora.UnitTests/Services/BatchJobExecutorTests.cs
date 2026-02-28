using Valora.Application.Common.Interfaces;
using Moq;
using Microsoft.Extensions.Logging;
using Valora.Application.Services;
using Valora.Application.Services.BatchJobs;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services;

public class BatchJobExecutorTests
{
    private readonly Mock<IEventDispatcher> _eventDispatcherMock = new();
    private readonly Mock<IBatchJobRepository> _jobRepositoryMock = new();
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
            _loggerMock.Object,
            _eventDispatcherMock.Object);
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
        Assert.Equal(BatchJobStatus.Completed, job.Status);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldHandleProcessorFailure()
    {
        var executor = CreateExecutor();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _cityIngestionProcessorMock.Setup(x => x.ProcessAsync(job, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Processor Error"));

        await executor.ProcessNextJobAsync();

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        Assert.Equal("Job failed due to an internal error.", job.Error);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldFail_WhenNoProcessorFound()
    {
        // Setup executor with empty processors list
        var executor = new BatchJobExecutor(_jobRepositoryMock.Object, new List<IBatchJobProcessor>(), _loggerMock.Object, _eventDispatcherMock.Object);
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await executor.ProcessNextJobAsync();

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        Assert.Equal("Job failed due to an internal error.", job.Error);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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
        var executor = new BatchJobExecutor(_jobRepositoryMock.Object, processors, _loggerMock.Object, _eventDispatcherMock.Object);

        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Processing };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await executor.ProcessNextJobAsync();

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        // SingleOrDefault throws InvalidOperationException when > 1 match
        // The executor catches Exception and sets a generic Error message
        Assert.NotNull(job.Error);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        Assert.Equal("Job cancelled by user.", job.Error);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
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
                // Simulate processor marking job as Failed (or some other status) internally without throwing
                j.Status = BatchJobStatus.Failed;
                j.Error = "Custom error";
            })
            .Returns(Task.CompletedTask);

        await executor.ProcessNextJobAsync();

        // Should REMAIN Failed, not be overwritten to Completed
        Assert.Equal(BatchJobStatus.Failed, job.Status);
        Assert.Equal("Custom error", job.Error);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
