using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Services;
using Valora.Application.Services.BatchJobs;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services;

public class BatchJobServiceTests
{
    private readonly Mock<IBatchJobRepository> _jobRepositoryMock = new();
    private readonly Mock<ILogger<BatchJobService>> _loggerMock = new();
    private readonly Mock<IBatchJobProcessor> _cityIngestionProcessorMock = new();
    private readonly List<IBatchJobProcessor> _processors = new();

    public BatchJobServiceTests()
    {
        _cityIngestionProcessorMock.Setup(x => x.JobType).Returns(BatchJobType.CityIngestion);
        _processors.Add(_cityIngestionProcessorMock.Object);
    }

    private BatchJobService CreateService()
    {
        return new BatchJobService(
            _jobRepositoryMock.Object,
            _processors,
            _loggerMock.Object);
    }

    [Fact]
    public async Task EnqueueJobAsync_ShouldAddJobAndReturnDto()
    {
        var service = CreateService();
        var type = BatchJobType.CityIngestion;
        var target = "Amsterdam";

        var result = await service.EnqueueJobAsync(type, target);

        Assert.Equal(target, result.Target);
        Assert.Equal(type.ToString(), result.Type);
        Assert.Equal(BatchJobStatus.Pending.ToString(), result.Status);
        _jobRepositoryMock.Verify(x => x.AddAsync(It.IsAny<BatchJob>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetRecentJobsAsync_ShouldReturnDtos()
    {
        var service = CreateService();
        var jobs = new List<BatchJob>
        {
            new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Completed }
        };
        _jobRepositoryMock.Setup(x => x.GetRecentJobsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobs);

        var result = await service.GetRecentJobsAsync();

        Assert.Single(result);
        Assert.Equal("Amsterdam", result[0].Target);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldDoNothing_WhenNoPendingJobs()
    {
        var service = CreateService();
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync((BatchJob?)null);

        await service.ProcessNextJobAsync();

        _jobRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<BatchJob>(), It.IsAny<CancellationToken>()), Times.Never);
        _cityIngestionProcessorMock.Verify(x => x.ProcessAsync(It.IsAny<BatchJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldCallCorrectProcessor()
    {
        var service = CreateService();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Pending };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await service.ProcessNextJobAsync();

        _cityIngestionProcessorMock.Verify(x => x.ProcessAsync(job, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(BatchJobStatus.Completed, job.Status);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldHandleProcessorFailure()
    {
        var service = CreateService();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Pending };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _cityIngestionProcessorMock.Setup(x => x.ProcessAsync(job, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Processor Error"));

        await service.ProcessNextJobAsync();

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        Assert.Equal("Processor Error", job.Error);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldFail_WhenNoProcessorFound()
    {
        // Setup service with empty processors list
        var service = new BatchJobService(_jobRepositoryMock.Object, new List<IBatchJobProcessor>(), _loggerMock.Object);
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Pending };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await service.ProcessNextJobAsync();

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        Assert.Contains("System configuration error: processor missing", job.Error);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldFail_WhenMultipleProcessorsFound()
    {
        // Setup service with duplicate processors
        var processor1 = new Mock<IBatchJobProcessor>();
        processor1.Setup(x => x.JobType).Returns(BatchJobType.CityIngestion);
        var processor2 = new Mock<IBatchJobProcessor>();
        processor2.Setup(x => x.JobType).Returns(BatchJobType.CityIngestion);

        var processors = new List<IBatchJobProcessor> { processor1.Object, processor2.Object };
        var service = new BatchJobService(_jobRepositoryMock.Object, processors, _loggerMock.Object);

        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Pending };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        await service.ProcessNextJobAsync();

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        // SingleOrDefault throws InvalidOperationException when > 1 match
        // The service catches Exception and sets Error = ex.Message
        // InvalidOperationException usually has "Sequence contains more than one matching element" or similar
        Assert.NotNull(job.Error);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldHandleCancellation()
    {
        var service = CreateService();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Pending };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _cityIngestionProcessorMock.Setup(x => x.ProcessAsync(job, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await service.ProcessNextJobAsync();

        Assert.Equal(BatchJobStatus.Failed, job.Status);
        Assert.Equal("Job cancelled by user.", job.Error);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ProcessNextJobAsync_ShouldNotOverwriteStatus_IfChangedByProcessor()
    {
        var service = CreateService();
        var job = new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Pending };
        _jobRepositoryMock.Setup(x => x.GetNextPendingJobAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        _cityIngestionProcessorMock.Setup(x => x.ProcessAsync(job, It.IsAny<CancellationToken>()))
            .Callback<BatchJob, CancellationToken>((j, ct) =>
            {
                // Simulate processor marking job as Failed (or some other status) internally without throwing
                // (Though usually failure implies throwing, maybe partial success or custom status)
                j.Status = BatchJobStatus.Failed;
                j.Error = "Custom error";
            })
            .Returns(Task.CompletedTask);

        await service.ProcessNextJobAsync();

        // Should REMAIN Failed, not be overwritten to Completed
        Assert.Equal(BatchJobStatus.Failed, job.Status);
        Assert.Equal("Custom error", job.Error);
        _jobRepositoryMock.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
