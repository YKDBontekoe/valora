using System.Text.Json;
using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.DTOs.Ai;
using Valora.Application.DTOs.Map;
using Valora.Application.Services;

namespace Valora.UnitTests.Services;

public class ContextAnalysisServiceTests
{
    private readonly Mock<IAiService> _aiServiceMock = new();
    private readonly Mock<IMapService> _mapServiceMock = new();

    private ContextAnalysisService CreateService()
    {
        return new ContextAnalysisService(_aiServiceMock.Object, _mapServiceMock.Object);
    }

    [Fact]
    public async Task AnalyzeReportAsync_GeneratesCorrectPrompt_WithFullData()
    {
        // Arrange
        var service = CreateService();
        var report = CreateFullReportDto();

        string capturedPrompt = "";
        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "detailed_analysis", It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((prompt, sys, model, ct) => capturedPrompt = prompt)
            .ReturnsAsync("Analysis Result");

        // Act
        await service.AnalyzeReportAsync(report, CancellationToken.None);

        // Assert
        Assert.Contains("<context_report>", capturedPrompt);
        Assert.Contains("<address>Damrak 1, Amsterdam</address>", capturedPrompt);
        Assert.Contains("<composite_score>78</composite_score>", capturedPrompt);
    }

    [Fact]
    public async Task AnalyzeReportAsync_TruncatesLongInput()
    {
        // Arrange
        var service = CreateService();
        var longAddress = new string('A', 300); // Exceeds default 200 char limit
        var report = CreateFullReportDto() with
        {
            Location = new ResolvedLocationDto("q", longAddress, 0, 0, null, null, null, null, null, null, null, null, null)
        };

        string capturedPrompt = "";
        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "detailed_analysis", It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((prompt, sys, model, ct) => capturedPrompt = prompt)
            .ReturnsAsync("Result");

        // Act
        await service.AnalyzeReportAsync(report, CancellationToken.None);

        // Assert
        Assert.Contains(new string('A', 200), capturedPrompt);
        Assert.DoesNotContain(new string('A', 201), capturedPrompt);
    }

    [Fact]
    public async Task ParseMapQueryPlan_HandlesMarkdownAndJsonElements()
    {
        // Arrange
        var service = CreateService();
        var request = new MapQueryRequest { Query = "test" };
        var jsonResponse = """
        ```json
        {
          "explanation": "Test explanation",
          "actions": [
            { "type": "set_overlay", "parameters": { "metric": "CrimeRate" } },
            { "type": "zoom_to", "parameters": { "lat": 52.5, "lon": 4.5, "zoom": 10 } }
          ]
        }
        ```
        """;

        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "map_query", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        _mapServiceMock.Setup(x => x.GetMapOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            MapOverlayMetric.CrimeRate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayDto>());

        // Act
        var result = await service.PlanMapQueryAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Test explanation", result.Explanation);
        Assert.Equal(52.5, result.SuggestCenterLat);
        Assert.Equal(4.5, result.SuggestCenterLon);
        Assert.Equal(10, result.SuggestZoom);

        _mapServiceMock.Verify(x => x.GetMapOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            MapOverlayMetric.CrimeRate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlanMapQueryAsync_HandlesMalformedJson_Gracefully()
    {
        // Arrange
        var service = CreateService();
        var request = new MapQueryRequest { Query = "test" };
        var jsonResponse = "This is not JSON";

        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "map_query", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await service.PlanMapQueryAsync(request, CancellationToken.None);

        // Assert
        Assert.Contains("I processed your request but encountered an issue", result.Explanation);
        Assert.Null(result.Overlays);
    }

    [Fact]
    public async Task PlanMapQueryAsync_HandlesShowAmenities_WithJsonElementArray()
    {
        // Arrange
        var service = CreateService();
        var request = new MapQueryRequest { Query = "Schools and Parks" };
        var jsonResponse = """
        {
          "actions": [
            { "type": "show_amenities", "parameters": { "types": ["school", "park"] } }
          ]
        }
        """;

        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "map_query", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        _mapServiceMock.Setup(x => x.GetMapAmenitiesAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapAmenityDto>());

        // Act
        await service.PlanMapQueryAsync(request, CancellationToken.None);

        // Assert
        _mapServiceMock.Verify(x => x.GetMapAmenitiesAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.Is<List<string>>(l => l.Contains("school") && l.Contains("park")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlanMapQueryAsync_HandlesShowAmenities_WithStringString()
    {
        // Arrange
        var service = CreateService();
        var request = new MapQueryRequest { Query = "Schools" };
        // Simulating scenario where LLM might return a string instead of array (resilience)
        var jsonResponse = """
        {
          "actions": [
            { "type": "show_amenities", "parameters": { "types": "school,park" } }
          ]
        }
        """;

        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "map_query", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        // Act
        await service.PlanMapQueryAsync(request, CancellationToken.None);

        // Assert
        _mapServiceMock.Verify(x => x.GetMapAmenitiesAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            It.Is<List<string>>(l => l.Contains("school") && l.Contains("park")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlanMapQueryAsync_HandlesSetOverlay_WithJsonElementString()
    {
        // Arrange
        var service = CreateService();
        var request = new MapQueryRequest { Query = "Crime" };

        var jsonResponse = """
        {
          "actions": [
            { "type": "set_overlay", "parameters": { "metric": "CrimeRate" } }
          ]
        }
        """;

        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "map_query", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        _mapServiceMock.Setup(x => x.GetMapOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            MapOverlayMetric.CrimeRate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayDto>());

        // Act
        await service.PlanMapQueryAsync(request, CancellationToken.None);

        // Assert
        _mapServiceMock.Verify(x => x.GetMapOverlaysAsync(
            It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),
            MapOverlayMetric.CrimeRate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlanMapQueryAsync_HandlesZoomTo_WithJsonElementNumbers()
    {
        // Arrange
        var service = CreateService();
        var request = new MapQueryRequest { Query = "Zoom" };
        var jsonResponse = """
        {
          "actions": [
            { "type": "zoom_to", "parameters": { "lat": 52.123, "lon": 4.123, "zoom": 14.5 } }
          ]
        }
        """;

        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "map_query", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await service.PlanMapQueryAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal(52.123, result.SuggestCenterLat);
        Assert.Equal(4.123, result.SuggestCenterLon);
        Assert.Equal(14.5, result.SuggestZoom);
    }

    [Fact]
    public async Task PlanMapQueryAsync_IgnoresInvalidActionsAndParameters()
    {
        // Arrange
        var service = CreateService();
        var request = new MapQueryRequest { Query = "Test" };
        var jsonResponse = """
        {
          "actions": [
            { "type": "unknown_action", "parameters": {} },
            { "type": "set_overlay", "parameters": { "metric": "InvalidMetric" } },
            { "type": "set_overlay", "parameters": {} },
            { "type": "zoom_to", "parameters": { "lat": "invalid" } }
          ]
        }
        """;

        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "map_query", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await service.PlanMapQueryAsync(request, CancellationToken.None);

        // Assert
        // Should verify no services called
        _mapServiceMock.Verify(x => x.GetMapOverlaysAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<MapOverlayMetric>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Null(result.SuggestCenterLat);
    }

    private static ContextReportDto CreateFullReportDto()
    {
        return new ContextReportDto(
            Location: new ResolvedLocationDto("q", "Damrak 1, Amsterdam", 52.37, 4.89, null, null, "Muni", "Amsterdam", "Dist", "Centrum", "Neigh", "Oude Zijde", "1012LG"),
            SocialMetrics: new List<ContextMetricDto>
            {
                new ContextMetricDto(Key: "restaurants", Label: "Restaurants", Value: 15, Unit: "count", Score: 85, Source: "OSM", Note: null)
            },
            CrimeMetrics: new List<ContextMetricDto>
            {
                new ContextMetricDto(Key: "crime_rate", Label: "Crime Rate", Value: 100, Unit: "risk", Score: 90, Source: "Police", Note: null)
            },
            DemographicsMetrics: new List<ContextMetricDto>(),
            HousingMetrics: new List<ContextMetricDto>(),
            MobilityMetrics: new List<ContextMetricDto>(),
            AmenityMetrics: new List<ContextMetricDto>(),
            EnvironmentMetrics: new List<ContextMetricDto>(),
            CompositeScore: 78,
            CategoryScores: new Dictionary<string, double>
            {
                { "Social", 80 },
                { "Safety", 75 }
            },
            Sources: new List<SourceAttributionDto>(),
            Warnings: new List<string>()
        );
    }
}
