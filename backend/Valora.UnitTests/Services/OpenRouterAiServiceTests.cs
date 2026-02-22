using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Valora.Application.Common.Interfaces;
using Valora.Infrastructure.Services;
using Xunit;

namespace Valora.UnitTests.Services;

public class OpenRouterAiServiceTests
{
    private readonly Mock<IAiModelService> _mockAiModelService;
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly IConfiguration _config;

    public OpenRouterAiServiceTests()
    {
        _mockAiModelService = new Mock<IAiModelService>();
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "OPENROUTER_API_KEY", "test-key" },
                { "OPENROUTER_BASE_URL", "https://api.test" },
                { "OPENROUTER_SITE_URL", "https://valora.app" },
                { "OPENROUTER_SITE_NAME", "Valora" }
            })
            .Build();
    }

    [Fact]
    public async Task GetAvailableModelsAsync_ReturnsModels_WhenResponseIsSuccessful()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var responseJson = new
        {
            data = new[]
            {
                new {
                    id = "model-1",
                    name = "Model 1",
                    description = "Desc 1",
                    context_length = 8192,
                    pricing = new { prompt = "0.0001", completion = "0.0002" }
                },
                new {
                    id = "model-2",
                    name = "Model 2",
                    description = "Desc 2",
                    context_length = 16384,
                    pricing = new { prompt = "0.0003", completion = "0.0004" }
                }
            }
        };

        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonSerializer.Serialize(responseJson))
            });

        var client = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        var sut = new OpenRouterAiService(_config, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await sut.GetAvailableModelsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("model-1", result[0].Id);
        Assert.Equal(0.0001m, result[0].PromptPrice);
        Assert.Equal(0.0002m, result[0].CompletionPrice);
        Assert.Equal("model-2", result[1].Id);
    }

    [Fact]
    public async Task GetAvailableModelsAsync_ReturnsEmptyList_WhenDataMissing()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{}") // Empty JSON object
            });

        var client = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        var sut = new OpenRouterAiService(_config, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object, _mockHttpClientFactory.Object);

        // Act
        var result = await sut.GetAvailableModelsAsync();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableModelsAsync_Throws_OnFailure()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        var client = new HttpClient(mockHandler.Object);
        _mockHttpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(client);

        var sut = new OpenRouterAiService(_config, NullLogger<OpenRouterAiService>.Instance, _mockAiModelService.Object, _mockHttpClientFactory.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetAvailableModelsAsync());
    }
}
