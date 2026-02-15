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
        var responseData = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(responseData);
        // Note: New API returns paginated object with Items
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
        var registerResponse = await Client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = emailToDelete,
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });

        Assert.True(registerResponse.IsSuccessStatusCode, $"Registration failed: {registerResponse.StatusCode} {await registerResponse.Content.ReadAsStringAsync()}");

        var usersResponse = await Client.GetAsync("/api/admin/users");
        var usersData = await usersResponse.Content.ReadFromJsonAsync<PaginatedUsersResponse>();
        var userToDelete = usersData!.Items.First(u => u.Email == emailToDelete);

        // Act
        var deleteResponse = await Client.DeleteAsync($"/api/admin/users/{userToDelete.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var finalUsersResponse = await Client.GetAsync("/api/admin/users");
        var finalUsersData = await finalUsersResponse.Content.ReadFromJsonAsync<PaginatedUsersResponse>();
        Assert.DoesNotContain(finalUsersData!.Items, u => u.Email == emailToDelete);
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

    [Fact]
    public async Task DeleteUser_Self_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        var usersResponse = await Client.GetAsync("/api/admin/users");
        var usersData = await usersResponse.Content.ReadFromJsonAsync<PaginatedUsersResponse>();
        var adminUser = usersData!.Items.First(u => u.Email == "admin@example.com");

        // Act
        var response = await Client.DeleteAsync($"/api/admin/users/{adminUser.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("You cannot delete your own account.", body);
    }

    private record PaginatedUsersResponse(List<AdminUserDto> Items);
}
