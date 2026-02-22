using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Application.Enrichment.Builders;
using Xunit;

namespace Valora.UnitTests.Enrichment;

public class ContextReportBuilderTests
{
    [Fact]
    public void Build_ConstructsValidReport_WithAllMetrics()
    {
        // Arrange
        var location = new ResolvedLocationDto(
            "Test Input",
            "Damrak 1, Amsterdam",
            52.370,
            4.895,
            121000,
            487000,
            "GM0363",
            "Amsterdam",
            "WK036300",
            "Burgwallen-Oude Zijde",
            "BU03630000",
            "Kop Zeedijk",
            "1012HG");

        var sourceData = new ContextSourceData(
            new NeighborhoodStatsDto(
                "BU03630000", "Buurt", 1000, 5000, 300000, 10, 500, 500,
                150, 100, 300, 250, 200, 400, 300, 300, 2.2, "ZeerStedelijk", 30.0, 35.0,
                20, 40, 40, 60, 40, 30, 10, 80, 20, 40, 0.8, 100, 500,
                1.0, 0.5, 0.8, 1.2, 5.0, DateTimeOffset.UtcNow),
            new CrimeStatsDto(45, 10, 5, 20, 10, -5.0, DateTimeOffset.UtcNow),
            new AmenityStatsDto(5, 2, 3, 4, 10, 150.0, 0.8, DateTimeOffset.UtcNow),
            new AirQualitySnapshotDto("ST01", "Amsterdam-Vondelpark", 500, 8.0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 15.0, 20.0),
            new List<SourceAttributionDto> { new("CBS", "https://cbs.nl", "CC-BY", DateTimeOffset.UtcNow) },
            new List<string>()
        );

        var warnings = new List<string>();

        // Act
        var report = ContextReportBuilder.Build(location, sourceData, warnings);

        // Assert
        Assert.NotNull(report);
        Assert.Equal(location, report.Location);
        Assert.NotNull(report.SocialMetrics);
        Assert.NotNull(report.CrimeMetrics);
        Assert.NotNull(report.EnvironmentMetrics);

        // Check Composite Score Calculation (rough check to ensure it's running)
        Assert.InRange(report.CompositeScore, 0, 100);

        // Check Sources
        Assert.Contains(report.Sources, s => s.Source == "CBS");
    }
}
