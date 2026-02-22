using System.Net.Http;
using System.Net;
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
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;

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
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient());

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
    public async Task ChatAsync_FallsBack_WhenPrimaryModelFails()
    {
        // Arrange
        _mockAiModelService
            .Setup(x => x.GetModelsForIntentAsync("chat", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("primary-model", new List<string> { "fallback-model" }));

        // Primary fails with 500
        _server
            .Given(Request.Create()
                .WithPath("/chat/completions")
                .UsingPost()
                .WithBody(new WireMock.Matchers.WildcardMatcher("*primary-model*")))
            .RespondWith(Response.Create()
                .WithStatusCode(500));

        // Fallback succeeds
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

        var sut = new OpenRouterAiService(_validConfig, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await sut.ChatAsync("Hello", null, "chat");

        // Assert
        Assert.Equal("Response from fallback", result);

        // Verify both models were tried by checking the log entries
        Assert.Contains(_server.LogEntries, e => e.RequestMessage.BodyData!.BodyAsString!.Contains("primary-model"));
        Assert.Contains(_server.LogEntries, e => e.RequestMessage.BodyData!.BodyAsString!.Contains("fallback-model"));
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
    public async Task ChatAsync_TriggersFallback_OnEmptyResponse()
    {
        // Arrange
        _mockAiModelService
            .Setup(x => x.GetModelsForIntentAsync("chat", It.IsAny<CancellationToken>()))
            .ReturnsAsync(("primary-model", new List<string> { "fallback-model" }));

        // Primary returns empty content
        _server
            .Given(Request.Create()
                .WithPath("/chat/completions")
                .UsingPost()
                .WithBody(new WireMock.Matchers.WildcardMatcher("*primary-model*")))
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{ ""choices"": [] }")); // SDK might treat this as empty string

        // Fallback succeeds
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

        var sut = new OpenRouterAiService(_validConfig, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await sut.ChatAsync("Hello", null, "chat");

        // Assert
        Assert.Equal("Response from fallback", result);
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

        var sut = new OpenRouterAiService(_validConfig, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object, _mockHttpClientFactory.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => sut.ChatAsync("Hello", null, "chat"));
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}
