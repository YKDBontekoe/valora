using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class ProfileEndpointTests
{
    private readonly HttpClient _client;
    private readonly Mock<IIdentityService> _mockIdentityService = new();

    public ProfileEndpointTests(TestDatabaseFixture fixture)
    {
        var factory = fixture.Factory!.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => _mockIdentityService.Object);
            });
        });

        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync()
    {
        var email = "test@example.com";
        var password = "Password123!";

        // We need a real user for the login endpoint to work if we are not mocking auth completely.
        // But here we might want to mock the login too or just rely on the fact that AuthEndpoints uses IAuthService which uses IIdentityService.
        // If we mock IIdentityService, we must mock CheckPasswordAsync and GetUserByEmailAsync for login to work.

        _mockIdentityService.Setup(x => x.GetUserByEmailAsync(email))
            .ReturnsAsync(new ApplicationUser { Id = "user123", Email = email });
        _mockIdentityService.Setup(x => x.CheckPasswordAsync(email, password))
            .ReturnsAsync(true);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { Email = email, Password = password });
        loginResponse.EnsureSuccessStatusCode();
        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth!.Token);
    }

    [Fact]
    public async Task GetProfile_ReturnsOk()
    {
        await AuthenticateAsync();
        _mockIdentityService.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new ApplicationUser { Email = "test@example.com", FirstName = "John" });

        var response = await _client.GetAsync("/api/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = await response.Content.ReadFromJsonAsync<UserProfileDto>();
        Assert.Equal("John", profile?.FirstName);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsOk()
    {
        await AuthenticateAsync();
        _mockIdentityService.Setup(x => x.UpdateProfileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(Application.Common.Models.Result.Success());

        var response = await _client.PutAsJsonAsync("/api/profile", new UpdateProfileDto { FirstName = "New", DefaultRadiusMeters = 1000 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_ReturnsBadRequest_WhenServiceFails()
    {
        await AuthenticateAsync();
        _mockIdentityService.Setup(x => x.UpdateProfileAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync(Application.Common.Models.Result.Failure(new[] { "Error" }));

        var response = await _client.PutAsJsonAsync("/api/profile", new UpdateProfileDto { FirstName = "New", DefaultRadiusMeters = 1000 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ChangePassword_ReturnsOk()
    {
        await AuthenticateAsync();
        _mockIdentityService.Setup(x => x.ChangePasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(Application.Common.Models.Result.Success());

        var response = await _client.PostAsJsonAsync("/api/profile/change-password", new ChangePasswordDto
        {
            CurrentPassword = "OldPassword123!",
            NewPassword = "NewPassword123!",
            ConfirmNewPassword = "NewPassword123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
