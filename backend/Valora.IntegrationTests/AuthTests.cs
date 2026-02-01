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
    public async Task Register_WithInvalidEmail_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = "invalid-email",
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_ReturnsBadRequest()
    {
        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", new RegisterDto
        {
            Email = "weakpass@example.com",
            Password = "weak",
            ConfirmPassword = "weak"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsToken()
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
}
