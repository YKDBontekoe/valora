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

        // Check Categories
        Assert.Contains("<score category=\"Social\">80</score>", capturedPrompt);
        Assert.Contains("<score category=\"Safety\">75</score>", capturedPrompt);

        // Check Metrics
        Assert.Contains("<metric category=\"Social\" label=\"Restaurants\">15 count (Score: 85)</metric>", capturedPrompt);
        Assert.Contains("<metric category=\"Safety\" label=\"Crime Rate\">100 risk (Score: 90)</metric>", capturedPrompt);
    }

    [Fact]
    public async Task AnalyzeReportAsync_GeneratesCorrectPrompt_WithMinimalData()
    {
        // Arrange
        var service = CreateService();
        var report = new ContextReportDto(
            Location: new ResolvedLocationDto("q", "Address", 52.0, 4.0, null, null, "Muni", "MuniName", "Dist", "DistName", "Neigh", "NeighName", "1234AB"),
            SocialMetrics: new List<ContextMetricDto>(),
            CrimeMetrics: new List<ContextMetricDto>(),
            DemographicsMetrics: new List<ContextMetricDto>(),
            HousingMetrics: new List<ContextMetricDto>(),
            MobilityMetrics: new List<ContextMetricDto>(),
            AmenityMetrics: new List<ContextMetricDto>(),
            EnvironmentMetrics: new List<ContextMetricDto>(),
            CompositeScore: 50,
            CategoryScores: new Dictionary<string, double>(),
            Sources: new List<SourceAttributionDto>(),
            Warnings: new List<string>()
        );

        string capturedPrompt = "";
        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "detailed_analysis", It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((prompt, sys, model, ct) => capturedPrompt = prompt)
            .ReturnsAsync("Analysis Result");

        // Act
        await service.AnalyzeReportAsync(report, CancellationToken.None);

        // Assert
        Assert.Contains("<context_report>", capturedPrompt);
        Assert.Contains("<address>Address</address>", capturedPrompt);
        // Should handle empty categories gracefully
        Assert.Contains("<category_scores>", capturedPrompt);
        Assert.Contains("</category_scores>", capturedPrompt);
        Assert.DoesNotContain("<score category=", capturedPrompt);
    }

    [Fact]
    public async Task AnalyzeReportAsync_SanitizesInputs()
    {
         // Arrange
        var service = CreateService();
        var report = CreateFullReportDto();

        // Inject malicious/special chars into a metric
        var maliciousMetric = new ContextMetricDto(Key: "test_key", Label: "Bad <Script> Label & More", Value: 5, Unit: "Unit", Score: 10, Source: "Source", Note: null);
        report = report with { SocialMetrics = new List<ContextMetricDto> { maliciousMetric } };

        string capturedPrompt = "";
        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "detailed_analysis", It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((prompt, sys, model, ct) => capturedPrompt = prompt)
            .ReturnsAsync("Analysis Result");

        // Act
        await service.AnalyzeReportAsync(report, CancellationToken.None);

        // Assert
        // < and > should be escaped to &lt; and &gt;
        Assert.Contains("label=\"Bad &lt;Script&gt; Label &amp; More\"", capturedPrompt);
        Assert.Contains(">5 Unit (Score: 10)<", capturedPrompt);
    }

    [Fact]
    public async Task PlanMapQueryAsync_ExecutesValidPlan()
    {
        // Arrange
        var service = CreateService();
        var request = new MapQueryRequest { Query = "Show me safe areas", CenterLat = 52.0, CenterLon = 4.0, Zoom = 12 };

        var jsonResponse = """
        {
          "explanation": "Here are the safe areas.",
          "actions": [
            { "type": "set_overlay", "parameters": { "metric": "CrimeRate" } }
          ]
        }
        """;

        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "map_query", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        _mapServiceMock.Setup(x => x.GetMapOverlaysAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), MapOverlayMetric.CrimeRate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayDto> { new MapOverlayDto("id", "name", "CrimeRate", 10, "Low", default) });

        // Act
        var result = await service.PlanMapQueryAsync(request, CancellationToken.None);

        // Assert
        Assert.Equal("Here are the safe areas.", result.Explanation);
        Assert.NotNull(result.Overlays);
        Assert.Single(result.Overlays);
        _mapServiceMock.Verify(x => x.GetMapOverlaysAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), MapOverlayMetric.CrimeRate, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PlanMapQueryAsync_HandlesMultipleActions()
    {
        // Arrange
        var service = CreateService();
        var request = new MapQueryRequest { Query = "Show me schools in safe areas", CenterLat = 52.0, CenterLon = 4.0, Zoom = 12 };

        var jsonResponse = """
        {
          "explanation": "Showing schools and safety.",
          "actions": [
            { "type": "set_overlay", "parameters": { "metric": "CrimeRate" } },
            { "type": "show_amenities", "parameters": { "types": ["school"] } }
          ]
        }
        """;

        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), "map_query", It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        _mapServiceMock.Setup(x => x.GetMapOverlaysAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), MapOverlayMetric.CrimeRate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapOverlayDto>());

        _mapServiceMock.Setup(x => x.GetMapAmenitiesAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<MapAmenityDto>());

        // Act
        var result = await service.PlanMapQueryAsync(request, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Overlays);
        Assert.NotNull(result.Amenities);
        _mapServiceMock.Verify(x => x.GetMapOverlaysAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), MapOverlayMetric.CrimeRate, It.IsAny<CancellationToken>()), Times.Once);
        _mapServiceMock.Verify(x => x.GetMapAmenitiesAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.Is<List<string>>(l => l.Contains("school")), It.IsAny<CancellationToken>()), Times.Once);
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
