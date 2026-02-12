using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class AiEndpointTests
{
    private readonly HttpClient _client;
    private readonly Mock<IAiService> _mockAiService = new();

    public AiEndpointTests(TestDatabaseFixture fixture)
    {
        // Create a separate factory for this test to mock services
        var factory = fixture.Factory!.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => _mockAiService.Object);
            });
        });

        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync()
    {
        var email = $"ai-test-{Guid.NewGuid()}@example.com";
        var password = "Password123!";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            Email = email,
            Password = password,
            ConfirmPassword = password
        });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            Email = email,
            Password = password
        });
        loginResponse.EnsureSuccessStatusCode();

        var authResponse = await loginResponse.Content.ReadFromJsonAsync<AuthResponseDto>();
        if (authResponse != null)
        {
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authResponse.Token);
        }
    }

    [Fact]
    public async Task Chat_ReturnsOk_WithResponse()
    {
        // Arrange
        await AuthenticateAsync();
        var request = new AiChatRequest { Prompt = "Hello", Model = "openai/gpt-4o" };
        _mockAiService
            .Setup(x => x.ChatAsync("Hello", "openai/gpt-4o", It.IsAny<CancellationToken>()))
            .ReturnsAsync("AI Response");

        // Act
        var response = await _client.PostAsJsonAsync("/api/ai/chat", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AiChatResponse>();
        Assert.Equal("AI Response", result?.Response);
    }

    [Fact]
    public async Task Chat_ReturnsBadRequest_WhenPromptIsEmpty()
    {
        // Arrange
        await AuthenticateAsync();
        var request = new AiChatRequest { Prompt = "", Model = "openai/gpt-4o" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ai/chat", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Helper record
    record AiChatResponse(string Response);
}
