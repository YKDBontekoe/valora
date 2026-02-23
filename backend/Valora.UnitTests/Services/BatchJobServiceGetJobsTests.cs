using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.Services;
using Valora.Application.Services.BatchJobs;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services;

public class BatchJobServiceGetJobsTests
{
    private readonly Mock<IBatchJobRepository> _jobRepositoryMock = new();
    private readonly Mock<ILogger<BatchJobService>> _loggerMock = new();
    private readonly List<IBatchJobProcessor> _processors = new();

    private BatchJobService CreateService()
    {
        return new BatchJobService(
            _jobRepositoryMock.Object,
            _processors,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetJobsAsync_ShouldPassSearchAndSortToRepository()
    {
        // Arrange
        var service = CreateService();
        var search = "Amsterdam";
        var sort = "createdAt_asc";
        var page = 1;
        var pageSize = 10;

        var jobs = new List<BatchJob>
        {
            new BatchJob { Type = BatchJobType.CityIngestion, Target = "Amsterdam", Status = BatchJobStatus.Completed }
        };
        var paginatedList = new PaginatedList<BatchJob>(jobs, 1, 1, 10);

        _jobRepositoryMock.Setup(x => x.GetJobsAsync(
            page,
            pageSize,
            It.IsAny<BatchJobStatus?>(),
            It.IsAny<BatchJobType?>(),
            search,
            sort,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(paginatedList);

        // Act
        var result = await service.GetJobsAsync(page, pageSize, null, null, search, sort);

        // Assert
        _jobRepositoryMock.Verify(x => x.GetJobsAsync(
            page,
            pageSize,
            null,
            null,
            search,
            sort,
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.Single(result.Items);
        Assert.Equal("Amsterdam", result.Items[0].Target);
    }
}
