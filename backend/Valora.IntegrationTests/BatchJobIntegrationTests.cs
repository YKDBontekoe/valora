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
        await base.InitializeAsync();
        DbContext.BatchJobs.RemoveRange(DbContext.BatchJobs);
        await DbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task GetJobs_ShouldReturnPaginatedJobs()
    {
        await AuthenticateAsAdminAsync();

        var jobs = new List<BatchJob>();
        for (int i = 0; i < 15; i++)
        {
            jobs.Add(new BatchJob
            {
                Type = i % 2 == 0 ? BatchJobType.CityIngestion : BatchJobType.MapGeneration,
                Status = i % 3 == 0 ? BatchJobStatus.Completed : BatchJobStatus.Pending,
                Target = $"City {i}",
                Progress = 0,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        DbContext.BatchJobs.AddRange(jobs);
        await DbContext.SaveChangesAsync();

        var response = await Client.GetAsync("/api/admin/jobs?page=1&pageSize=10");

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
        await AuthenticateAsAdminAsync();

        var jobs = new List<BatchJob>
        {
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Completed, Target = "C1", CreatedAt = DateTime.UtcNow },
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Pending, Target = "C2", CreatedAt = DateTime.UtcNow },
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Failed, Target = "C3", CreatedAt = DateTime.UtcNow }
        };
        DbContext.BatchJobs.AddRange(jobs);
        await DbContext.SaveChangesAsync();

        var response = await Client.GetAsync("/api/admin/jobs?status=Completed");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<BatchJobSummaryDto>>();

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, item => Assert.Equal(BatchJobStatus.Completed, item.Status));
    }

    [Fact]
    public async Task GetJobs_ShouldFilterByType()
    {
        await AuthenticateAsAdminAsync();

        var jobs = new List<BatchJob>
        {
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Completed, Target = "C1", CreatedAt = DateTime.UtcNow },
            new() { Type = BatchJobType.MapGeneration, Status = BatchJobStatus.Pending, Target = "C2", CreatedAt = DateTime.UtcNow }
        };
        DbContext.BatchJobs.AddRange(jobs);
        await DbContext.SaveChangesAsync();

        var response = await Client.GetAsync("/api/admin/jobs?type=MapGeneration");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<BatchJobSummaryDto>>();

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, item => Assert.Equal(BatchJobType.MapGeneration, item.Type));
    }

    [Fact]
    public async Task GetJobs_ShouldFilterBySearch()
    {
        await AuthenticateAsAdminAsync();

        var jobs = new List<BatchJob>
        {
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Completed, Target = "Amsterdam", CreatedAt = DateTime.UtcNow },
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Completed, Target = "Rotterdam", CreatedAt = DateTime.UtcNow }
        };
        DbContext.BatchJobs.AddRange(jobs);
        await DbContext.SaveChangesAsync();

        var response = await Client.GetAsync("/api/admin/jobs?q=Amst"); // unambiguous substring search

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<BatchJobSummaryDto>>();

        Assert.NotNull(result);
        Assert.Equal(1, result.TotalCount);
        Assert.Equal("Amsterdam", result.Items[0].Target);
    }

    [Fact]
    public async Task GetJobs_ShouldFilterByStatusAndType()
    {
        await AuthenticateAsAdminAsync();

        var jobs = new List<BatchJob>
        {
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Completed, Target = "Match", CreatedAt = DateTime.UtcNow },
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Failed, Target = "StatusMiss", CreatedAt = DateTime.UtcNow },
            new() { Type = BatchJobType.MapGeneration, Status = BatchJobStatus.Completed, Target = "TypeMiss", CreatedAt = DateTime.UtcNow }
        };
        DbContext.BatchJobs.AddRange(jobs);
        await DbContext.SaveChangesAsync();

        var response = await Client.GetAsync("/api/admin/jobs?status=Completed&type=CityIngestion");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<BatchJobSummaryDto>>();

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.NotEmpty(result.Items);
        Assert.All(result.Items, item =>
        {
            Assert.Equal(BatchJobStatus.Completed, item.Status);
            Assert.Equal(BatchJobType.CityIngestion, item.Type);
        });
    }

    [Theory]
    [InlineData("createdAt_asc", "A", "B", "C")] // A created first
    [InlineData("createdAt_desc", "C", "B", "A")] // C created last
    // Status: Pending(0), Processing(1), Completed(2), Failed(3)
    // A=Completed(2), B=Failed(3), C=Pending(0)
    [InlineData("status_asc", "C", "A", "B")] // 0 -> 2 -> 3
    [InlineData("status_desc", "B", "A", "C")] // 3 -> 2 -> 0
    // Type: CityIngestion(0), MapGeneration(1), AllCitiesIngestion(2)
    // A=City(0), B=All(2), C=Map(1)
    [InlineData("type_asc", "A", "C", "B")] // 0 -> 1 -> 2
    [InlineData("type_desc", "B", "C", "A")] // 2 -> 1 -> 0
    [InlineData("target_asc", "A", "B", "C")] // Alpha
    [InlineData("target_desc", "C", "B", "A")] // Reverse Alpha
    public async Task GetJobs_ShouldSort(string sort, string first, string second, string third)
    {
        await AuthenticateAsAdminAsync();
        var now = DateTime.UtcNow;

        var jobs = new List<BatchJob>
        {
            new() {
                Target = "A",
                Type = BatchJobType.CityIngestion,
                Status = BatchJobStatus.Completed,
                CreatedAt = now.AddMinutes(-30)
            },
            new() {
                Target = "B",
                Type = BatchJobType.AllCitiesIngestion,
                Status = BatchJobStatus.Failed,
                CreatedAt = now.AddMinutes(-20)
            },
            new() {
                Target = "C",
                Type = BatchJobType.MapGeneration,
                Status = BatchJobStatus.Pending,
                CreatedAt = now.AddMinutes(-10)
            }
        };
        DbContext.BatchJobs.AddRange(jobs);
        await DbContext.SaveChangesAsync();

        var response = await Client.GetAsync($"/api/admin/jobs?sort={sort}");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<BatchJobSummaryDto>>();

        Assert.NotNull(result);
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(first, result.Items[0].Target);
        Assert.Equal(second, result.Items[1].Target);
        Assert.Equal(third, result.Items[2].Target);
    }

    [Fact]
    public async Task GetJobs_ShouldReturnBadRequest_ForInvalidFilters()
    {
        await AuthenticateAsAdminAsync();

        var response = await Client.GetAsync("/api/admin/jobs?status=InvalidStatus");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        response = await Client.GetAsync("/api/admin/jobs?type=InvalidType");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetJobs_WithPageBeyondRange_ReturnsEmptyList()
    {
        await AuthenticateAsAdminAsync();

        var jobs = new List<BatchJob>
        {
            new() { Type = BatchJobType.CityIngestion, Status = BatchJobStatus.Completed, Target = "C1", CreatedAt = DateTime.UtcNow }
        };
        DbContext.BatchJobs.AddRange(jobs);
        await DbContext.SaveChangesAsync();

        var response = await Client.GetAsync("/api/admin/jobs?page=1000&pageSize=10");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<BatchJobSummaryDto>>();

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.False(result.HasNextPage);
        Assert.True(result.HasPreviousPage);
    }
}
