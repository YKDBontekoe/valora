using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestcontainersDatabase")]
public class UserProfileEndpointTests : BaseTestcontainersIntegrationTest
{
    public UserProfileEndpointTests(TestcontainersDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetProfile_ReturnsEmpty_WhenNotSet()
    {
        // Arrange
        var email = $"profile-test-{Guid.NewGuid()}@example.com";
        await AuthenticateAsync(email);

        // Act
        var response = await Client.GetAsync("/api/user/ai-profile");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserAiProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal(string.Empty, profile.Preferences);
    }

    [Fact]
    public async Task UpdateProfile_SavesPreferences()
    {
        // Arrange
        var email = $"profile-update-{Guid.NewGuid()}@example.com";
        await AuthenticateAsync(email);
        var dto = new UserAiProfileDto { Preferences = "I like parks.", HouseholdProfile = "Family of 4" };

        // Act
        var response = await Client.PutAsJsonAsync("/api/user/ai-profile", dto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<UserAiProfileDto>();
        Assert.Equal("I like parks.", updated?.Preferences);
        Assert.Equal("Family of 4", updated?.HouseholdProfile);
        Assert.Equal(1, updated?.Version);

        // Verify retrieval
        var getResponse = await Client.GetAsync("/api/user/ai-profile");
        var retrieved = await getResponse.Content.ReadFromJsonAsync<UserAiProfileDto>();
        Assert.Equal("I like parks.", retrieved?.Preferences);
    }

    [Fact]
    public async Task DeleteProfile_RemovesData()
    {
        // Arrange
        var email = $"profile-delete-{Guid.NewGuid()}@example.com";
        await AuthenticateAsync(email);
        await Client.PutAsJsonAsync("/api/user/ai-profile", new UserAiProfileDto { Preferences = "Temp" });

        // Act
        var response = await Client.DeleteAsync("/api/user/ai-profile");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify gone
        var getResponse = await Client.GetAsync("/api/user/ai-profile");
        var retrieved = await getResponse.Content.ReadFromJsonAsync<UserAiProfileDto>();
        Assert.Equal(string.Empty, retrieved?.Preferences);
    }

    [Fact]
    public async Task ExportProfile_ReturnsFile()
    {
        // Arrange
        var email = $"profile-export-{Guid.NewGuid()}@example.com";
        await AuthenticateAsync(email);
        await Client.PutAsJsonAsync("/api/user/ai-profile", new UserAiProfileDto { Preferences = "ExportData" });

        // Act
        var response = await Client.GetAsync("/api/user/ai-profile/export");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("ExportData", content);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    // --- Admin Endpoints ---

    private async Task<string> CreateTestUserWithProfileAsync(string email, string preferences)
    {
        await AuthenticateAsync(email);
        var response = await Client.PutAsJsonAsync("/api/user/ai-profile", new UserAiProfileDto { Preferences = preferences });
        response.EnsureSuccessStatusCode();

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            throw new Exception($"Failed to find newly created user {email} for test setup.");
        }
        return user.Id;
    }

    [Fact]
    public async Task AdminGetProfile_ShouldReturnUserProfile_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var userEmail = $"user-{Guid.NewGuid()}@test.com";
        var userId = await CreateTestUserWithProfileAsync(userEmail, "AdminTestPrefs");

        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync($"/api/admin/users/{userId}/ai-profile");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserAiProfileDto>();
        Assert.NotNull(profile);
        Assert.Equal("AdminTestPrefs", profile.Preferences);
    }

    [Fact]
    public async Task AdminGetProfile_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        var userEmail = $"user-{Guid.NewGuid()}@test.com";
        var userId = await CreateTestUserWithProfileAsync(userEmail, "ForbiddenTestPrefs");

        // Authenticate as a different regular user
        await AuthenticateAsync($"other-{Guid.NewGuid()}@test.com");

        // Act
        var response = await Client.GetAsync($"/api/admin/users/{userId}/ai-profile");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminDeleteProfile_ShouldRemoveUserProfile_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var userEmail = $"user-delete-{Guid.NewGuid()}@test.com";
        var userId = await CreateTestUserWithProfileAsync(userEmail, "AdminDeleteTestPrefs");

        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.DeleteAsync($"/api/admin/users/{userId}/ai-profile");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify gone
        var getResponse = await Client.GetAsync($"/api/admin/users/{userId}/ai-profile");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var profile = await getResponse.Content.ReadFromJsonAsync<UserAiProfileDto>();
        Assert.Equal(string.Empty, profile?.Preferences);
    }

    [Fact]
    public async Task AdminDeleteProfile_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        var userEmail = $"user-delete-forbidden-{Guid.NewGuid()}@test.com";
        var userId = await CreateTestUserWithProfileAsync(userEmail, "ForbiddenDeleteTestPrefs");

        // Authenticate as a different regular user
        await AuthenticateAsync($"other-{Guid.NewGuid()}@test.com");

        // Act
        var response = await Client.DeleteAsync($"/api/admin/users/{userId}/ai-profile");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminExportProfile_ShouldReturnFile_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var userEmail = $"user-export-{Guid.NewGuid()}@test.com";
        var userId = await CreateTestUserWithProfileAsync(userEmail, "AdminExportTestPrefs");

        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync($"/api/admin/users/{userId}/ai-profile/export");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("AdminExportTestPrefs", content);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task AdminExportProfile_ShouldReturnForbidden_WhenNotAdmin()
    {
        // Arrange
        var userEmail = $"user-export-forbidden-{Guid.NewGuid()}@test.com";
        var userId = await CreateTestUserWithProfileAsync(userEmail, "ForbiddenExportTestPrefs");

        // Authenticate as a different regular user
        await AuthenticateAsync($"other-{Guid.NewGuid()}@test.com");

        // Act
        var response = await Client.GetAsync($"/api/admin/users/{userId}/ai-profile/export");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
