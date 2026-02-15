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
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/stats");

        // Assert
        response.EnsureSuccessStatusCode();
        var stats = await response.Content.ReadFromJsonAsync<AdminStatsDto>();
        Assert.NotNull(stats);
    }

    [Fact]
    public async Task GetUsers_AsAdmin_ReturnsOk()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/users");

        // Assert
        response.EnsureSuccessStatusCode();
        var users = await response.Content.ReadFromJsonAsync<List<AdminUserDto>>();
        Assert.NotNull(users);
        Assert.NotEmpty(users); // Should include the admin user
    }

    [Fact]
    public async Task GetStats_AsRegularUser_ReturnsForbidden()
    {
        // Arrange
        await AuthenticateAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/stats");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_AsAdmin_RemovesUser()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Create a user to delete
        var emailToDelete = "todelete@example.com";
        await Client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = emailToDelete,
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });

        var usersResponse = await Client.GetFromJsonAsync<List<AdminUserDto>>("/api/admin/users");
        var userToDelete = usersResponse!.First(u => u.Email == emailToDelete);

        // Act
        var deleteResponse = await Client.DeleteAsync($"/api/admin/users/{userToDelete.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var finalUsers = await Client.GetFromJsonAsync<List<AdminUserDto>>("/api/admin/users");
        Assert.DoesNotContain(finalUsers!, u => u.Email == emailToDelete);
    }

    [Fact]
    public async Task DeleteUser_NonExistent_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/admin/users/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
