using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.Common.Interfaces;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

public class BatchJobIntegrationTests : BaseIntegrationTest
{
    public BatchJobIntegrationTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    public override async Task InitializeAsync()
    {
        // Need to be careful with initialization as BaseIntegrationTest might have its own logic.
        // But here we just want to clear BatchJobs.
        // Calling base.InitializeAsync() first.
        await base.InitializeAsync();
        DbContext.BatchJobs.RemoveRange(DbContext.BatchJobs);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetJobs_ShouldReturnPaginatedJobs()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Seeding jobs
        var jobs = new List<BatchJob>();
        for (int i = 0; i < 15; i++)
        {
            jobs.Add(new BatchJob
            {
                Type = i % 2 == 0 ? BatchJobType.CityIngestion : BatchJobType.MapGeneration,
                Status = i % 3 == 0 ? BatchJobStatus.Completed : BatchJobStatus.Pending,
                Target = $"City {i}",
                Progress = 0,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i) // Newer first
            });
        }
        DbContext.BatchJobs.AddRange(jobs);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/jobs?page=1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<BatchJobSummaryDto>>();

        Assert.NotNull(result);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(10, result.Items.Count);
        Assert.Equal(1, result.PageIndex);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
    }

    [Fact]
    public async Task GetJobs_ShouldFilterByStatus()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        var jobs = new List<BatchJob>
        {
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Completed, Target = "C1", CreatedAt = DateTime.UtcNow },
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Pending, Target = "C2", CreatedAt = DateTime.UtcNow },
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Failed, Target = "C3", CreatedAt = DateTime.UtcNow }
        };
        DbContext.BatchJobs.AddRange(jobs);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/jobs?status=Completed");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<BatchJobSummaryDto>>();

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, item => Assert.Equal("Completed", item.Status));
    }

    [Fact]
    public async Task GetJobs_ShouldFilterByType()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        var jobs = new List<BatchJob>
        {
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Completed, Target = "C1", CreatedAt = DateTime.UtcNow },
            new() { Type = BatchJobType.MapGeneration, Status = BatchJobStatus.Pending, Target = "C2", CreatedAt = DateTime.UtcNow }
        };
        DbContext.BatchJobs.AddRange(jobs);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/jobs?type=MapGeneration");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<BatchJobSummaryDto>>();

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, item => Assert.Equal("MapGeneration", item.Type));
    }

    [Fact]
    public async Task GetJobs_ShouldIgnoreInvalidFilters()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        var jobs = new List<BatchJob>
        {
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Completed, Target = "C1", CreatedAt = DateTime.UtcNow },
            new() { Type = BatchJobType.MapGeneration, Status = BatchJobStatus.Pending, Target = "C2", CreatedAt = DateTime.UtcNow }
        };
        DbContext.BatchJobs.AddRange(jobs);
        await DbContext.SaveChangesAsync();

        // Act - Invalid Status (Should return all)
        var response = await Client.GetAsync("/api/admin/jobs?status=InvalidStatus");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<BatchJobSummaryDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);

        // Act - Invalid Type (Should return all)
        response = await Client.GetAsync("/api/admin/jobs?type=InvalidType");
        response.EnsureSuccessStatusCode();
        result = await response.Content.ReadFromJsonAsync<PaginatedResponse<BatchJobSummaryDto>>();
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
    }

    [Fact]
    public async Task GetJobs_ShouldFilterByStatusAndType()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        var jobs = new List<BatchJob>
        {
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Completed, Target = "Match", CreatedAt = DateTime.UtcNow },
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Failed, Target = "StatusMiss", CreatedAt = DateTime.UtcNow },
            new() { Type = BatchJobType.MapGeneration, Status = BatchJobStatus.Completed, Target = "TypeMiss", CreatedAt = DateTime.UtcNow }
        };
        DbContext.BatchJobs.AddRange(jobs);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/jobs?status=Completed&type=CityIngestion");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<BatchJobSummaryDto>>();

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item =>
        {
            Assert.Equal("Completed", item.Status);
            Assert.Equal("CityIngestion", item.Type);
        });
    }

    [Fact]
    public async Task GetJobs_WithPageBeyondRange_ReturnsEmptyList()
    {
         // Arrange
        await AuthenticateAsAdminAsync();

        // Ensure at least one job exists
        var jobs = new List<BatchJob>
        {
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Completed, Target = "C1", CreatedAt = DateTime.UtcNow }
        };
        DbContext.BatchJobs.AddRange(jobs);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/jobs?page=1000&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<BatchJobSummaryDto>>();

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.False(result.HasNextPage);
    }
}
