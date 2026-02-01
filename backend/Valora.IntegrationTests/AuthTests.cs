using System.Net;
using System.Net.Http.Json;
using Valora.Application.DTOs;
using Xunit;

namespace Valora.IntegrationTests;

public class AuthTests : BaseIntegrationTest
{
    public AuthTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = "newuser@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        // Arrange
        await Client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = "duplicate@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = "duplicate@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithMismatchedPasswords_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = "mismatch@example.com",
            Password = "Password123!",
            ConfirmPassword = "Password456!"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsRefreshAndAccessToken()
    {
        // Arrange
        var email = "loginuser@example.com";
        var password = "Password123!";
        await Client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginDto(email, password));

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        Assert.Equal(email, result.Email);
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
    {
        // Arrange
        var email = "wrongpass@example.com";
        var password = "Password123!";
        await Client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginDto(email, "WrongPassword!"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistentUser_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/login", new LoginDto("ghost@example.com", "Password123!"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewAccessAndRefreshToken()
    {
        // Arrange
        var email = "refreshflow@example.com";
        var password = "Password123!";
        await Client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new LoginDto(email, password));
        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDto(authData!.RefreshToken));

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result.Token));
        Assert.False(string.IsNullOrEmpty(result.RefreshToken));
        Assert.NotEqual(authData.RefreshToken, result.RefreshToken); // Ensure Rotation
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDto("InvalidTokenString"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
