using System.Text.Json;
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
    private readonly IConfiguration _validConfig;
    private readonly Mock<IAiModelService> _mockAiModelService;

    public AiServiceTests()
    {
        _server = WireMockServer.Start();

        var inMemorySettings = new Dictionary<string, string?> {
            {"OPENROUTER_API_KEY", "test-key"},
            {"OPENROUTER_BASE_URL", _server.Url},
            {"OPENROUTER_SITE_URL", "https://valora.app"},
            {"OPENROUTER_SITE_NAME", "Valora"}
        };

        _validConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _mockAiModelService = new Mock<IAiModelService>();

        // Default mock setup
        _mockAiModelService
            .Setup(x => x.GetModelsForIntentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("openrouter/default", new List<string>()));
    }

    [Fact]
    public async Task ChatAsync_UsesCorrectModelForIntent()
    {
        // Arrange
        _mockAiModelService
            .Setup(x => x.GetModelsForIntentAsync("chat", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("gpt-4o", new List<string>()));

        _server
            .Given(Request.Create().WithPath("/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{
                    ""choices"": [{
                        ""message"": { ""role"": ""assistant"", ""content"": ""Response from gpt-4o"" }
                    }]
                }"));

        var sut = new OpenRouterAiService(_validConfig, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object);

        // Act
        var result = await sut.ChatAsync("Hello", null, "chat");

        // Assert
        Assert.Equal("Response from gpt-4o", result);

        var request = _server.LogEntries.Last().RequestMessage;
        var body = request.BodyData?.BodyAsString;
        Assert.Contains("gpt-4o", body!);
    }

    [Fact]
    public async Task ChatAsync_FallsBack_WhenPrimaryModelFails()
    {
        // Arrange
        _mockAiModelService
            .Setup(x => x.GetModelsForIntentAsync("chat", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("primary-model", new List<string> { "fallback-model" }));

        // Primary fails with 500. Matcher: Body contains "primary-model"
        _server
            .Given(Request.Create()
                .WithPath("/chat/completions")
                .UsingPost()
                .WithBody(new WireMock.Matchers.WildcardMatcher("*primary-model*")))
            .RespondWith(Response.Create()
                .WithStatusCode(500));

        // Fallback succeeds. Matcher: Body contains "fallback-model"
        _server
            .Given(Request.Create()
                .WithPath("/chat/completions")
                .UsingPost()
                .WithBody(new WireMock.Matchers.WildcardMatcher("*fallback-model*")))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{
                    ""choices"": [{
                        ""message"": { ""role"": ""assistant"", ""content"": ""Response from fallback"" }
                    }]
                }"));

        var sut = new OpenRouterAiService(_validConfig, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object);

        // Act
        var result = await sut.ChatAsync("Hello", null, "chat");

        // Assert
        Assert.Equal("Response from fallback", result);

        // Verify both models were tried by checking the log entries
        Assert.Contains(_server.LogEntries, e => e.RequestMessage.BodyData!.BodyAsString!.Contains("primary-model"));
        Assert.Contains(_server.LogEntries, e => e.RequestMessage.BodyData!.BodyAsString!.Contains("fallback-model"));
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

        var sut = new OpenRouterAiService(customConfig, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object);

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
        Assert.Throws<InvalidOperationException>(() => new OpenRouterAiService(config, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object));
    }

    [Fact]
    public async Task ChatAsync_Throws_After_All_Models_Fail()
    {
        // Arrange
        _mockAiModelService
            .Setup(x => x.GetModelsForIntentAsync("chat", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("primary-model", new List<string> { "fallback-model" }));

        // Both fail
        _server
            .Given(Request.Create().WithPath("/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(500));

        var sut = new OpenRouterAiService(_validConfig, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => sut.ChatAsync("Hello", null, "chat"));
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}
