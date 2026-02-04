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
    private readonly OpenRouterAiService _sut;

    public AiServiceTests()
    {
        _server = WireMockServer.Start();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OPENROUTER_API_KEY", "test-key" },
                { "OPENROUTER_BASE_URL", _server.Url },
                // OPENROUTER_MODEL left empty to test default behavior
            })
            .Build();

        _sut = new OpenRouterAiService(config);
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

        // Act
        var result = await _sut.ChatAsync("Hello");

        // Assert
        Assert.Equal("Response from default model", result);

        var request = _server.LogEntries.First().RequestMessage;

        // Verify Headers
        Assert.Contains(request.Headers, h => h.Key == "HTTP-Referer");
        Assert.Contains(request.Headers, h => h.Key == "X-Title");

        // Verify Body DOES NOT contain model property
        // We check for "model" key string specifically
        var body = request.BodyData?.BodyAsString;
        Assert.NotNull(body);
        Assert.DoesNotContain("\"model\"", body);
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

        // Act
        var result = await _sut.ChatAsync("Hello", "gpt-4");

        // Assert
        Assert.Equal("Response from specific model", result);

        var request = _server.LogEntries.Last().RequestMessage;
        var body = request.BodyData?.BodyAsString;

        // Verify Body DOES contain model
        Assert.NotNull(body);
        Assert.Contains("model", body);
        Assert.Contains("gpt-4", body);
    }

    public void Dispose()
    {
        _server.Stop();
        _server.Dispose();
    }
}
