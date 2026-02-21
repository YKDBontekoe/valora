using Moq;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Services;

namespace Valora.UnitTests.Services;

public class ContextAnalysisServiceTests
{
    private readonly Mock<IAiService> _aiServiceMock = new();

    private ContextAnalysisService CreateService()
    {
        return new ContextAnalysisService(_aiServiceMock.Object);
    }

    [Fact]
    public async Task AnalyzeReportAsync_GeneratesCorrectPrompt_AndHandlesEmptyJson_WithFullData()
    {
        // Arrange
        var service = CreateService();
        var report = CreateFullReportDto();

        string capturedPrompt = "";
        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((prompt, sys, model, ct) => capturedPrompt = prompt)
            .ReturnsAsync("{}"); // Return empty JSON

        // Act
        var result = await service.AnalyzeReportAsync(report, CancellationToken.None);

        // Assert
        // Verify prompt content
        Assert.Contains("<context_report>", capturedPrompt);
        Assert.Contains("<address>Damrak 1, Amsterdam</address>", capturedPrompt);
        Assert.Contains("<composite_score>78</composite_score>", capturedPrompt);

        // Verify fallback logic for empty JSON
        Assert.Equal("Analysis not available.", result.Summary);
        Assert.Empty(result.TopPositives);
        Assert.Empty(result.TopConcerns);
        Assert.Equal(0, result.Confidence);
        Assert.Contains("Could not parse", result.Disclaimer);

        // Check Categories in prompt
        Assert.Contains("<score category=\"Social\">80</score>", capturedPrompt);
        Assert.Contains("<score category=\"Safety\">75</score>", capturedPrompt);

        // Check Metrics in prompt
        Assert.Contains("<metric category=\"Social\" label=\"Restaurants\">15 count (Score: 85)</metric>", capturedPrompt);
        Assert.Contains("<metric category=\"Safety\" label=\"Crime Rate\">100 risk (Score: 90)</metric>", capturedPrompt);

        // Check JSON Request instructions
        Assert.Contains("provide a structured analysis in JSON format", capturedPrompt);
    }

    [Fact]
    public async Task AnalyzeReportAsync_ParsesStructuredJson()
    {
        // Arrange
        var service = CreateService();
        var report = CreateFullReportDto();
        var jsonResponse = """
            ```json
            {
                "summary": "Great place.",
                "topPositives": ["Walkable", "Green"],
                "topConcerns": ["Noisy"],
                "confidence": 90,
                "disclaimer": "No guarantees."
            }
            ```
            """;

        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await service.AnalyzeReportAsync(report, CancellationToken.None);

        // Assert
        Assert.Equal("Great place.", result.Summary);
        Assert.Equal(2, result.TopPositives.Count);
        Assert.Contains("Walkable", result.TopPositives);
        Assert.Single(result.TopConcerns);
        Assert.Equal("Noisy", result.TopConcerns[0]);
        Assert.Equal(90, result.Confidence);
        Assert.Equal("No guarantees.", result.Disclaimer);
    }

    [Fact]
    public async Task AnalyzeReportAsync_HandlesMalformedJson_FallbackToSummary()
    {
        // Arrange
        var service = CreateService();
        var report = CreateFullReportDto();
        var rawText = "This is not JSON. It is just a summary.";

        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawText);

        // Act
        var result = await service.AnalyzeReportAsync(report, CancellationToken.None);

        // Assert
        Assert.Equal(rawText, result.Summary);
        Assert.Empty(result.TopPositives);
        Assert.Empty(result.TopConcerns);
        Assert.Equal(0, result.Confidence);
        Assert.Contains("Could not parse", result.Disclaimer);
    }

    [Fact]
    public async Task AnalyzeReportAsync_ClampsConfidence()
    {
        // Arrange
        var service = CreateService();
        var report = CreateFullReportDto();
        var jsonResponse = """
            {
                "summary": "Test",
                "confidence": 150,
                "topPositives": [],
                "topConcerns": [],
                "disclaimer": ""
            }
            """;

        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await service.AnalyzeReportAsync(report, CancellationToken.None);

        // Assert
        Assert.Equal(100, result.Confidence);
    }

    [Fact]
    public async Task AnalyzeReportAsync_SanitizesAndTruncatesInputs()
    {
         // Arrange
        var service = CreateService();
        var report = CreateFullReportDto();

        // Create a string longer than 200 chars
        var longString = new string('a', 250);
        // Inject malicious/special chars into a metric
        var maliciousMetric = new ContextMetricDto("test_key", "Bad <Script> Label & More " + longString, 5, "Unit", 10, "Source", null);
        report = report with { SocialMetrics = new List<ContextMetricDto> { maliciousMetric } };

        string capturedPrompt = "";
        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((prompt, sys, model, ct) => capturedPrompt = prompt)
            .ReturnsAsync("{}");

        // Act
        await service.AnalyzeReportAsync(report, CancellationToken.None);

        // Assert
        // < and > should be escaped to &lt; and &gt;
        Assert.Contains("label=\"Bad &lt;Script&gt; Label &amp; More", capturedPrompt);

        // Verify truncation: The prompt should NOT contain the full 250 'a's.
        // Logic: Input (truncated to 200) -> Regex Replace -> HTML Escape
        // 200 chars max. "Bad <Script> Label & More " is ~26 chars. So we expect around 174 'a's.
        // It definitely shouldn't have 250.
        Assert.DoesNotContain(longString, capturedPrompt);
    }

    [Fact]
    public async Task AnalyzeReportAsync_SanitizesAndTruncatesInputs_WithNullInput()
    {
         // Arrange
        var service = CreateService();
        var report = CreateFullReportDto();

        // Inject malicious/special chars into a metric
        var maliciousMetric = new ContextMetricDto("test_key", null!, 5, "Unit", 10, "Source", null);
        report = report with { SocialMetrics = new List<ContextMetricDto> { maliciousMetric } };

        string capturedPrompt = "";
        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((prompt, sys, model, ct) => capturedPrompt = prompt)
            .ReturnsAsync("{}");

        // Act
        await service.AnalyzeReportAsync(report, CancellationToken.None);

        // Assert
        Assert.Contains("label=\"\"", capturedPrompt);
    }

    [Fact]
    public async Task AnalyzeReportAsync_HandlesPartialJson_WithNullLists()
    {
        // Arrange
        var service = CreateService();
        var report = CreateFullReportDto();
        // JSON where lists are null/missing
        var jsonResponse = """
            {
                "summary": "Valid summary.",
                "confidence": 50
            }
            """;

        _aiServiceMock.Setup(x => x.ChatAsync(It.IsAny<string>(), It.IsAny<string>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jsonResponse);

        // Act
        var result = await service.AnalyzeReportAsync(report, CancellationToken.None);

        // Assert
        Assert.Equal("Valid summary.", result.Summary);
        Assert.NotNull(result.TopPositives);
        Assert.Empty(result.TopPositives);
        Assert.NotNull(result.TopConcerns);
        Assert.Empty(result.TopConcerns);
        Assert.Equal(50, result.Confidence);
        Assert.Equal(string.Empty, result.Disclaimer);
    }

    private static ContextReportDto CreateFullReportDto()
    {
        return new ContextReportDto(
            Location: new ResolvedLocationDto("q", "Damrak 1, Amsterdam", 52.37, 4.89, null, null, "Muni", "Amsterdam", "Dist", "Centrum", "Neigh", "Oude Zijde", "1012LG"),
            SocialMetrics: new List<ContextMetricDto>
            {
                new("restaurants", "Restaurants", 15, "count", 85, "OSM", null)
            },
            CrimeMetrics: new List<ContextMetricDto>
            {
                new("crime_rate", "Crime Rate", 100, "risk", 90, "Police", null)
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
