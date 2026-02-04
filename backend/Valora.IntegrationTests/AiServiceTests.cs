using System.Text;
using Microsoft.Extensions.Configuration;
using Valora.Infrastructure.Services;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Matchers;

namespace Valora.IntegrationTests;

public class AiServiceTests : IDisposable
{
    private readonly WireMockServer _server;
    private readonly IConfiguration _validConfig;

    public AiServiceTests()
    {
        _server = WireMockServer.Start();

        _validConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OPENROUTER_API_KEY", "test-key" },
                { "OPENROUTER_BASE_URL", _server.Url },
                // OPENROUTER_MODEL left empty to test default behavior
            })
            .Build();
    }

    [Fact]
    public async Task ChatAsync_WithDefaultModel_ShouldOmitModelFieldInRequest()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{
                    ""id"": ""chatcmpl-123"",
                    ""choices"": [{
                        ""message"": { ""role"": ""assistant"", ""content"": ""Response from default model"" }
                    }]
                }"));

        var sut = new OpenRouterAiService(_validConfig);

        // Act
        var result = await sut.ChatAsync("Hello");

        // Assert
        Assert.Equal("Response from default model", result);

        var request = _server.LogEntries.First().RequestMessage;

        // Verify Headers (Defaults)
        Assert.NotNull(request.Headers);
        Assert.Contains(request.Headers!, h => h.Key == "HTTP-Referer" && h.Value.Contains("https://valora.app"));
        Assert.Contains(request.Headers!, h => h.Key == "X-Title" && h.Value.Contains("Valora"));

        // Verify Body DOES NOT contain model property
        var body = request.BodyData?.BodyAsString;
        Assert.NotNull(body);
        Assert.DoesNotContain("\"model\"", body);
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
                    ""id"": ""chatcmpl-123"",
                    ""choices"": [{
                        ""message"": { ""role"": ""assistant"", ""content"": ""Response"" }
                    }]
                }"));

        var sut = new OpenRouterAiService(customConfig);

        // Act
        await sut.ChatAsync("Hello");

        // Assert
        var request = _server.LogEntries.Last().RequestMessage;

        // Verify Headers
        Assert.NotNull(request.Headers);
        Assert.Contains(request.Headers!, h => h.Key == "HTTP-Referer" && h.Value.Contains("https://custom.com"));
        Assert.Contains(request.Headers!, h => h.Key == "X-Title" && h.Value.Contains("CustomApp"));
    }

    [Fact]
    public async Task ChatAsync_WithSpecificModel_ShouldIncludeModelField()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{
                    ""id"": ""chatcmpl-123"",
                    ""choices"": [{
                        ""message"": { ""role"": ""assistant"", ""content"": ""Response from specific model"" }
                    }]
                }"));

        var sut = new OpenRouterAiService(_validConfig);

        // Act
        var result = await sut.ChatAsync("Hello", "gpt-4");

        // Assert
        Assert.Equal("Response from specific model", result);

        var request = _server.LogEntries.Last().RequestMessage;
        var body = request.BodyData?.BodyAsString;

        // Verify Body DOES contain model
        Assert.NotNull(body);
        Assert.Contains("model", body);
        Assert.Contains("gpt-4", body);
    }

    [Fact]
    public void Constructor_Throws_WhenApiKeyMissing()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => new OpenRouterAiService(config));
    }

    [Fact]
    public async Task ChatAsync_HandlesEmptyResponse()
    {
        // Arrange
        _server
            .Given(Request.Create().WithPath("/chat/completions").UsingPost())
            .RespondWith(Response.Create()
                .WithStatusCode(200)
                .WithBody(@"{
                    ""choices"": []
                }"));

        var sut = new OpenRouterAiService(_validConfig);

        // Act
        var result = await sut.ChatAsync("Hello");

        // Assert
        Assert.Equal(string.Empty, result);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}
