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

    private BatchJobService CreateService()
    {
        return new BatchJobService(
            _jobRepositoryMock.Object,
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
}
