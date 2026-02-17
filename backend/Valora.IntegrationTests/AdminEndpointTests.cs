using System.Net;
using System.Net.Http.Json;
using Valora.Application.DTOs;
using Xunit;

namespace Valora.IntegrationTests;

public class AdminEndpointTests : BaseIntegrationTest
{
    public AdminEndpointTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetStats_AsAdmin_ReturnsOk()
    {
        await AuthenticateAsAdminAsync();
        var response = await Client.GetAsync("/api/admin/stats");
        response.EnsureSuccessStatusCode();
        var stats = await response.Content.ReadFromJsonAsync<AdminStatsDto>();
        Assert.NotNull(stats);
    }

    [Fact]
    public async Task GetUsers_AsAdmin_ReturnsOk()
    {
        await AuthenticateAsAdminAsync();
        var response = await Client.GetAsync("/api/admin/users");
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(responseData);
    }

    [Fact]
    public async Task GetStats_AsRegularUser_ReturnsForbidden()
    {
        await AuthenticateAsync();
        var response = await Client.GetAsync("/api/admin/stats");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_AsAdmin_RemovesUser()
    {
        await AuthenticateAsAdminAsync();
        var emailToDelete = "todelete@example.com";
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = emailToDelete,
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });
        Assert.True(registerResponse.IsSuccessStatusCode);

        var usersResponse = await Client.GetAsync("/api/admin/users");
        var usersData = await usersResponse.Content.ReadFromJsonAsync<PaginatedUsersResponse>();
        var userToDelete = usersData!.Items.First(u => u.Email == emailToDelete);

        var deleteResponse = await Client.DeleteAsync($"/api/admin/users/{userToDelete.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var finalUsersResponse = await Client.GetAsync("/api/admin/users");
        var finalUsersData = await finalUsersResponse.Content.ReadFromJsonAsync<PaginatedUsersResponse>();
        Assert.DoesNotContain(finalUsersData!.Items, u => u.Email == emailToDelete);
    }

    [Fact]
    public async Task Jobs_Endpoints_Work_Correctly()
    {
        await AuthenticateAsAdminAsync();

        var enqueueResponse = await Client.PostAsJsonAsync("/api/admin/jobs", new { Type = "CityIngestion", Target = "Amsterdam" });
        Assert.Equal(HttpStatusCode.Accepted, enqueueResponse.StatusCode);
        var job = await enqueueResponse.Content.ReadFromJsonAsync<IntegrationBatchJobDto>();
        Assert.NotNull(job);
        Assert.Equal("Amsterdam", job.Target);

        var listResponse = await Client.GetAsync("/api/admin/jobs?limit=5");
        listResponse.EnsureSuccessStatusCode();
        var jobs = await listResponse.Content.ReadFromJsonAsync<List<IntegrationBatchJobDto>>();
        Assert.NotNull(jobs);
        Assert.Contains(jobs, j => j.Id == job.Id);
    }

    [Fact]
    public async Task EnqueueJob_InvalidType_ReturnsBadRequest()
    {
        await AuthenticateAsAdminAsync();
        var response = await Client.PostAsJsonAsync("/api/admin/jobs", new { Type = "InvalidType", Target = "Target" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private record PaginatedUsersResponse(List<AdminUserDto> Items);
    private record IntegrationBatchJobDto(Guid Id, string Type, string Status, string Target);
}
