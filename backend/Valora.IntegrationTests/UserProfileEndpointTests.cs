using System.Net;
using System.Net.Http.Json;
using Valora.Application.DTOs;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class UserProfileEndpointTests
{
    private readonly HttpClient _client;

    public UserProfileEndpointTests(TestDatabaseFixture fixture)
    {
        _client = fixture.Factory!.CreateClient();
    }

    private async Task AuthenticateAsync(string email)
    {
        var password = "Password123!";
        await _client.PostAsJsonAsync("/api/auth/register", new { Email = email, Password = password, ConfirmPassword = password });
        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        loginResponse.EnsureSuccessStatusCode();
        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (authResponse != null)
        {
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
        }
    }

    [Fact]
    public async Task GetProfile_ReturnsEmpty_WhenNotSet()
    {
        // Arrange
        var email = $"profile-test-{Guid.NewGuid()}@example.com";
        await AuthenticateAsync(email);

        // Act
        var response = await _client.GetAsync("/api/user/ai-profile");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserAiProfileDto>();
        Assert.NotNull(profile);
        Assert.Empty(profile.Preferences);
    }

    [Fact]
    public async Task UpdateProfile_SavesPreferences()
    {
        // Arrange
        var email = $"profile-update-{Guid.NewGuid()}@example.com";
        await AuthenticateAsync(email);
        var dto = new UserAiProfileDto { Preferences = "I like parks.", HouseholdProfile = "Family of 4" };

        // Act
        var response = await _client.PutAsJsonAsync("/api/user/ai-profile", dto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<UserAiProfileDto>();
        Assert.Equal("I like parks.", updated?.Preferences);
        Assert.Equal("Family of 4", updated?.HouseholdProfile);
        Assert.Equal(1, updated?.Version);

        // Verify retrieval
        var getResponse = await _client.GetAsync("/api/user/ai-profile");
        var retrieved = await getResponse.Content.ReadFromJsonAsync<UserAiProfileDto>();
        Assert.Equal("I like parks.", retrieved?.Preferences);
    }

    [Fact]
    public async Task DeleteProfile_RemovesData()
    {
        // Arrange
        var email = $"profile-delete-{Guid.NewGuid()}@example.com";
        await AuthenticateAsync(email);
        await _client.PutAsJsonAsync("/api/user/ai-profile", new UserAiProfileDto { Preferences = "Temp" });

        // Act
        var response = await _client.DeleteAsync("/api/user/ai-profile");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify gone
        var getResponse = await _client.GetAsync("/api/user/ai-profile");
        var retrieved = await getResponse.Content.ReadFromJsonAsync<UserAiProfileDto>();
        Assert.Empty(retrieved?.Preferences ?? "");
    }

    [Fact]
    public async Task ExportProfile_ReturnsFile()
    {
        // Arrange
        var email = $"profile-export-{Guid.NewGuid()}@example.com";
        await AuthenticateAsync(email);
        await _client.PutAsJsonAsync("/api/user/ai-profile", new UserAiProfileDto { Preferences = "ExportData" });

        // Act
        var response = await _client.GetAsync("/api/user/ai-profile/export");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("ExportData", content);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
    }
}
