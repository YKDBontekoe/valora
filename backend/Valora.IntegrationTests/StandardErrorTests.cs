using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Valora.Application.DTOs;
using Valora.Application.DTOs.Shared;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class StandardErrorTests : IAsyncLifetime
{
    private readonly HttpClient _client;

    public StandardErrorTests(TestDatabaseFixture fixture)
    {
        var factory = fixture.Factory ?? throw new InvalidOperationException("Factory not initialized");
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Post_Login_InvalidCredentials_ReturnsProblemDetails()
    {
        // Arrange
        var loginDto = new LoginDto("invalid@example.com", "InvalidPass123!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginDto);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        var problem = document.RootElement;

        Assert.Equal(401, problem.GetProperty("status").GetInt32());
        Assert.True(problem.TryGetProperty("detail", out var detail));
        // Verify standard structure
        Assert.True(problem.TryGetProperty("traceId", out var traceId) || problem.TryGetProperty("extensions", out _));
    }

    [Fact]
    public async Task Post_Register_InvalidData_ReturnsValidationProblem()
    {
        // Arrange
        var invalidRegisterDto = new RegisterDto
        {
            Email = "invalid-email",
            Password = "short",
            ConfirmPassword = "mismatch"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", invalidRegisterDto);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var content = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(content);
        var problem = document.RootElement;

        Assert.Equal(400, problem.GetProperty("status").GetInt32());

        Assert.True(problem.TryGetProperty("errors", out var errors));
    }
}
