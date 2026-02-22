using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Valora.Application.DTOs;
using Xunit;
using Moq;
using Google.Apis.Auth;
using Valora.Application.Common.Interfaces.External;

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
    public async Task Login_PersistsHashedRefreshToken()
    {
        // Arrange
        var email = "hashedrefresh@example.com";
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

        var storedToken = await DbContext.RefreshTokens.SingleAsync();
        var expectedHash = Convert.ToBase64String(
            SHA256.HashData(Encoding.UTF8.GetBytes(result.RefreshToken)));

        Assert.Equal(expectedHash, storedToken.TokenHash);
        Assert.NotEqual(result.RefreshToken, storedToken.TokenHash);
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
    public async Task Refresh_WithRevokedToken_ReturnsUnauthorized()
    {
        // Arrange
        var email = "revokedrefresh@example.com";
        var password = "Password123!";
        await Client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });

        var loginResponse = await Client.PostAsJsonAsync("/api/auth/login", new LoginDto(email, password));
        var authData = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(authData);

        var storedToken = await DbContext.RefreshTokens.SingleAsync();
        storedToken.Revoked = DateTime.UtcNow;
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDto(authData.RefreshToken));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithInvalidToken_ReturnsUnauthorized()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequestDto("InvalidTokenString"));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExternalLogin_ValidGoogleToken_RegistersNewUserAndReturnsToken()
    {
        // Arrange
        var provider = "google";
        var idToken = "valid_google_token";
        var email = "googleuser@example.com";

        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "123456789",
            Email = email,
            Name = "Google User"
        };

        // Setup mock validator
        Factory.GoogleTokenValidatorMock
            .Setup(x => x.ValidateAsync(idToken))
            .ReturnsAsync(payload);

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/external-login", new ExternalLoginRequestDto(provider, idToken));

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
        Assert.False(string.IsNullOrEmpty(result.Token));

        // Verify user was created in DB
        var user = await DbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
        Assert.NotNull(user);
    }

    [Fact]
    public async Task ExternalLogin_ExistingUser_LogsInAndReturnsToken()
    {
        // Arrange
        var email = "existinggoogle@example.com";
        // Create user first
        await Client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = email,
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });

        var provider = "google";
        var idToken = "valid_google_token_existing";

        var payload = new GoogleJsonWebSignature.Payload
        {
            Subject = "987654321",
            Email = email,
            Name = "Existing User"
        };

        Factory.GoogleTokenValidatorMock
            .Setup(x => x.ValidateAsync(idToken))
            .ReturnsAsync(payload);

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/external-login", new ExternalLoginRequestDto(provider, idToken));

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();

        Assert.NotNull(result);
        Assert.Equal(email, result.Email);
    }

    [Fact]
    public async Task ExternalLogin_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var provider = "google";
        var idToken = "invalid_token";

        Factory.GoogleTokenValidatorMock
            .Setup(x => x.ValidateAsync(idToken))
            .ThrowsAsync(new InvalidJwtException("Invalid"));

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/external-login", new ExternalLoginRequestDto(provider, idToken));

        // Assert
        // The service throws ValidationException which usually maps to BadRequest in our middleware/filters
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
