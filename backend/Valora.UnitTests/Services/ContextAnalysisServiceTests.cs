using Moq;
using System.Text.Json;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs.Map;
using Valora.Application.Services;

namespace Valora.UnitTests.Services;

public class ContextAnalysisServiceTests
{
    private readonly Mock<IAiService> _aiServiceMock;
    private readonly ContextAnalysisService _service;

    public ContextAnalysisServiceTests()
    {
        _aiServiceMock = new Mock<IAiService>();
        _service = new ContextAnalysisService(_aiServiceMock.Object);
    }

    [Fact]
    public async Task PlanMapQueryAsync_ValidJson_ReturnsDto()
    {
        // Arrange
        var jsonResponse = @"{
            ""explanation"": ""Cheap areas."",
            ""targetLocation"": { ""lat"": 52.0, ""lon"": 4.0, ""zoom"": 12.0 },
            ""filter"": { ""metric"": ""PricePerSquareMeter"", ""amenityTypes"": [""park""] }
        }";

        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "map_query", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await _service.PlanMapQueryAsync("test query", null, CancellationToken.None);

        // Assert
        Assert.Equal("Cheap areas.", result.Explanation);
        Assert.NotNull(result.TargetLocation);
        Assert.Equal(52.0, result.TargetLocation!.Lat);
        Assert.Equal(MapOverlayMetric.PricePerSquareMeter, result.Filter!.Metric);
        Assert.Contains("park", result.Filter.AmenityTypes);
    }

    [Fact]
    public async Task PlanMapQueryAsync_InvalidJson_ReturnsFallback()
    {
        // Arrange
        var invalidResponse = "I don't understand.";

        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "map_query", It.IsAny<CancellationToken>()))
            .ReturnsAsync(invalidResponse);

        // Act
        var result = await _service.PlanMapQueryAsync("test query", null, CancellationToken.None);

        // Assert
        Assert.Equal(invalidResponse, result.Explanation);
        Assert.Null(result.TargetLocation);
        Assert.Null(result.Filter);
    }
}
