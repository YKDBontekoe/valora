using Microsoft.Extensions.Logging;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Models;
using Valora.Application.Services;
using Valora.Domain.Entities;

namespace Valora.UnitTests.Services;

public class BatchJobServiceGetJobsTests
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
    public async Task GetJobsAsync_ShouldPassSearchAndSortToRepository()
    {
        // Arrange
        var service = CreateService();
        var search = "Amsterdam";
        var sort = "createdAt_asc";
        var page = 1;
        var pageSize = 10;

        var dtos = new List<BatchJobSummaryDto>
        {
            new BatchJobSummaryDto(Guid.NewGuid(), BatchJobType.CityIngestion, BatchJobStatus.Completed, "Amsterdam", 100, null, null, DateTime.UtcNow, null, null)
        };
        var repoList = new PaginatedList<BatchJobSummaryDto>(dtos, 1, 1, 10);

        _jobRepositoryMock.Setup(x => x.GetJobSummariesAsync(
            page,
            pageSize,
            It.IsAny<BatchJobStatus?>(),
            It.IsAny<BatchJobType?>(),
            search,
            sort,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(repoList);

        // Act
        var result = await service.GetJobsAsync(page, pageSize, null, null, search, sort);

        // Assert
        _jobRepositoryMock.Verify(x => x.GetJobSummariesAsync(
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

    [Fact]
    public async Task GetJobsAsync_NullSearchAndSort_PassesNulls()
    {
        // Arrange
        var service = CreateService();
        var page = 1;
        var pageSize = 10;
        var repoList = new PaginatedList<BatchJobSummaryDto>(new List<BatchJobSummaryDto>(), 0, 1, 10);

        _jobRepositoryMock.Setup(x => x.GetJobSummariesAsync(
            page,
            pageSize,
            null,
            null,
            null,
            null,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(repoList);

        // Act
        await service.GetJobsAsync(page, pageSize, null, null, null, null);

        // Assert
        _jobRepositoryMock.Verify(x => x.GetJobSummariesAsync(
            page,
            pageSize,
            null,
            null,
            null,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetJobsAsync_EmptySearch_PassesEmpty()
    {
        // Arrange
        var service = CreateService();
        var search = "";
        var page = 1;
        var pageSize = 10;
        var repoList = new PaginatedList<BatchJobSummaryDto>(new List<BatchJobSummaryDto>(), 0, 1, 10);

        _jobRepositoryMock.Setup(x => x.GetJobSummariesAsync(
            page,
            pageSize,
            null,
            null,
            search,
            null,
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(repoList);

        // Act
        await service.GetJobsAsync(page, pageSize, null, null, search, null);

        // Assert
        _jobRepositoryMock.Verify(x => x.GetJobSummariesAsync(
            page,
            pageSize,
            null,
            null,
            search,
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetJobsAsync_ShouldThrow_WhenStatusInvalid()
    {
        // Arrange
        var service = CreateService();
        var invalidStatus = "InvalidStatus";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetJobsAsync(1, 10, status: invalidStatus));
    }

    [Fact]
    public async Task GetJobsAsync_ShouldThrow_WhenTypeInvalid()
    {
        // Arrange
        var service = CreateService();
        var invalidType = "InvalidType";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.GetJobsAsync(1, 10, type: invalidType));
    }
}
