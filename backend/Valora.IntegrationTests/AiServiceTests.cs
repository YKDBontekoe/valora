using Valora.Application.DTOs;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Infrastructure.Services;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace Valora.IntegrationTests;

public class AiServiceTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly Mock<IAiModelService> _mockAiModelService = new();
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory = new();
    private readonly IConfiguration _validConfig;

    public AiServiceTests()
    {
        _server = WireMockServer.Start();

        var inMemorySettings = new Dictionary<string, string?>
        {
            { "OPENROUTER_API_KEY", "test-key" },
            { "OPENROUTER_BASE_URL", _server.Url },
            { "OPENROUTER_SITE_URL", "https://test.com" },
            { "OPENROUTER_SITE_NAME", "TestApp" }
        };

        _validConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _mockAiModelService
            .Setup(x => x.GetModelForFeatureAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("openrouter/default");

        // Setup HttpClientFactory to return a client that points to WireMock
        var client = _server.CreateClient();
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);
    }

    [Fact]
    public async Task ChatAsync_UsesCorrectModelForFeature()
    {
        // Arrange
        _mockAiModelService
            .Setup(x => x.GetModelForFeatureAsync("chat", It.IsAny<CancellationToken>()))
            .ReturnsAsync("gpt-4o");

        _server
            .Given(Request.Create().WithPath("/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{
                    ""choices"": [{
                        ""message"": { ""role"": ""assistant"", ""content"": ""Response from gpt-4o"" }
                    }]
                }"));

        var sut = new OpenRouterAiService(_validConfig, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await sut.ChatAsync("Hello", null, "chat");

        // Assert
        Assert.Equal("Response from gpt-4o", result);

        var request = _server.LogEntries.Last().RequestMessage;
        var body = request.BodyData?.BodyAsString;
        Assert.Contains("gpt-4o", body!);
    }

    [Fact]
    public async Task ChatAsync_ThrowsOperationCanceledException_WhenCancelled()
    {
        var sut = new OpenRouterAiService(_validConfig, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object, _mockHttpClientFactory.Object);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.ChatAsync("Hello", null, "chat", cts.Token));
    }

    [Fact]
    public async Task ChatAsync_MapsBadRequest_ToHttpRequestException_WithBadRequestStatus()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(400)
                .WithBody("Bad Request"));

        var sut = new OpenRouterAiService(_validConfig, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object, _mockHttpClientFactory.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => sut.ChatAsync("Hello", null, "chat"));
        Assert.Equal(HttpStatusCode.BadRequest, ex.StatusCode);
    }

    [Fact]
    public async Task ChatAsync_WithCustomHeadersConfig_ShouldSendCustomHeaders()
    {
        // Arrange
        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OPENROUTER_API_KEY", "test-key" },
                { "OPENROUTER_BASE_URL", _server.Url },
                { "OPENROUTER_SITE_URL", "https://custom.com" },
                { "OPENROUTER_SITE_NAME", "CustomApp" }
            })
            .Build();

        _server
            .Given(Request.Create().WithPath("/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{
                    ""choices"": [{
                        ""message"": { ""role"": ""assistant"", ""content"": ""Response"" }
                    }]
                }"));

        var sut = new OpenRouterAiService(customConfig, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object, _mockHttpClientFactory.Object);

        // Act
        await sut.ChatAsync("Hello", null, "chat");

        // Assert
        var request = _server.LogEntries.Last().RequestMessage;
        Assert.NotNull(request.Headers);
        Assert.Contains(request.Headers!, h => h.Key == "HTTP-Referer" && h.Value.Contains("https://custom.com"));
        Assert.Contains(request.Headers!, h => h.Key == "X-Title" && h.Value.Contains("CustomApp"));
    }

    [Fact]
    public void Constructor_Throws_WhenApiKeyMissing()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new OpenRouterAiService(config, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object, _mockHttpClientFactory.Object));
    }

    [Fact]
    public async Task GetAvailableModelsAsync_ReturnsModels_WhenApiCallSucceeds()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/models").UsingGet())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{
                    ""data"": [{
                        ""id"": ""model-1"",
                        ""name"": ""Model 1"",
                        ""pricing"": { ""prompt"": ""0.1"", ""completion"": ""0.2"" },
                        ""context_length"": 1000
                    }]
                }"));

        var sut = new OpenRouterAiService(_validConfig, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await sut.GetAvailableModelsAsync();

        // Assert
        Assert.Single(result);
        var model = result.First();
        Assert.Equal("model-1", model.Id);
        Assert.Equal("Model 1", model.Name);
        Assert.Equal(0.1m, model.PromptPrice);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }

    [Fact]
    public async Task ChatAsync_ThrowsException_WhenResponseIsEmpty()
    {
        // Arrange
        _mockAiModelService
            .Setup(x => x.GetModelForFeatureAsync("chat", It.IsAny<CancellationToken>()))
            .ReturnsAsync("gpt-4o");

        _server
            .Given(Request.Create().WithPath("/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{ ""choices"": [] }")); // SDK treats empty choices as no content

        var sut = new OpenRouterAiService(_validConfig, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object, _mockHttpClientFactory.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => sut.ChatAsync("Hello", null, "chat"));
        Assert.Contains("AI model failed", ex.Message);
    }

    [Fact]
    public async Task ChatAsync_ThrowsHttpRequestException_WhenRateLimited()
    {
        // Arrange
        _mockAiModelService
            .Setup(x => x.GetModelForFeatureAsync("chat", It.IsAny<CancellationToken>()))
            .ReturnsAsync("gpt-4o");

        _server
            .Given(Request.Create().WithPath("/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(429)
                .WithBody("Too Many Requests"));

        var sut = new OpenRouterAiService(_validConfig, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object, _mockHttpClientFactory.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HttpRequestException>(() => sut.ChatAsync("Hello", null, "chat"));
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.StatusCode);
    }

    [Fact]
    public async Task ChatAsync_UsesConfiguredLLMParameters()
    {
        // Arrange
        var configDto = new AiModelConfigDto
        {
            Id = Guid.NewGuid(),
            Feature = "test-feature",
            ModelId = "gpt-4o-custom",
            IsEnabled = true,
            SystemPrompt = "Custom System Prompt",
            Temperature = 0.5,
            MaxTokens = 100
        };

        _mockAiModelService
            .Setup(x => x.GetConfigByFeatureAsync("test-feature", It.IsAny<CancellationToken>()))
            .ReturnsAsync(configDto);

        _server
            .Given(Request.Create().WithPath("/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{
                    ""choices"": [{
                        ""message"": { ""role"": ""assistant"", ""content"": ""Response from custom params"" }
                    }]
                }"));

        var sut = new OpenRouterAiService(_validConfig, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await sut.ChatAsync("Hello", "Default Prompt", "test-feature");

        // Assert
        Assert.Equal("Response from custom params", result);

        var request = _server.LogEntries.Last().RequestMessage;
        var body = request.BodyData?.BodyAsString;

        // Assert that the Custom System Prompt from the DB overrode the "Default Prompt" passed to the method
        Assert.Contains("Custom System Prompt", body!);
        Assert.DoesNotContain("Default Prompt", body!);

        // Assert parameters were injected
        Assert.Contains("\"temperature\":0.5", body!.Replace(" ", ""));
        Assert.Contains("\"max_completion_tokens\":100", body!.Replace(" ", ""));
        Assert.Contains("gpt-4o-custom", body!);
    }
}
