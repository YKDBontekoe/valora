using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.DTOs.Ai;
using Valora.Application.DTOs.Map;
using Valora.Application.Services;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class MapQueryIntegrationTests
{
    private readonly HttpClient _client;
    private readonly Mock<IAiService> _mockAiService = new();
    private readonly Mock<IMapService> _mockMapService = new();

    public MapQueryIntegrationTests(TestDatabaseFixture fixture)
    {
        var factory = fixture.Factory!.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped(_ => _mockAiService.Object);
                services.AddScoped(_ => _mockMapService.Object);
            });
        });

        _client = factory.CreateClient();
    }

    private async Task AuthenticateAsync()
    {
        var email = $"mapquery-test-{Guid.NewGuid()}@example.com";
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
    public async Task MapQuery_ReturnsOk_WithPlanAndData()
    {
        // Arrange
        await AuthenticateAsync();
        var request = new MapQueryRequest { Query = "Safe areas", CenterLat = 52.3, CenterLon = 4.9, Zoom = 12 };

        var jsonResponse = """
        {
          "explanation": "Showing safe areas.",
          "actions": [
            { "type": "set_overlay", "parameters": { "metric": "CrimeRate" } }
          ]
        }
        """;

        _mockAiService
            .Setup(x => x.ChatAsync(
                It.IsAny<string>(),
                ContextAnalysisService.MapQuerySystemPrompt,
                "map_query",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        _mockMapService
            .Setup(x => x.GetMapOverlaysAsync(
                It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
                MapOverlayMetric.CrimeRate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayDto>
            {
                new MapOverlayDto("id", "name", "CrimeRate", 10, "Low", default)
            });

        // Act
        var response = await _client.PostAsJsonAsync("/api/ai/map-query", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<MapQueryResponse>();
        Assert.NotNull(result);
        Assert.Equal("Showing safe areas.", result.Explanation);
        Assert.NotNull(result.Overlays);
        Assert.Single(result.Overlays);
    }

    [Fact]
    public async Task MapQuery_ReturnsBadRequest_WhenQueryIsEmpty()
    {
         // Arrange
        await AuthenticateAsync();
        var request = new MapQueryRequest { Query = "" };

        // Act
        var response = await _client.PostAsJsonAsync("/api/ai/map-query", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
