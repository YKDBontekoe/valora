using Valora.Domain.Models;
using Xunit;

namespace Valora.UnitTests.Domain.Models;

public class DomainModelTests
{
    [Fact]
    public void ContextMetricModel_Constructor_InitializesCorrectly()
    {
        // Parameterized constructor
        var metric = new ContextMetricModel("key", "label", 10.0, "unit", 100.0, "source", "note");
        Assert.Equal("key", metric.Key);
        Assert.Equal("label", metric.Label);
        Assert.Equal(10.0, metric.Value);
        Assert.Equal("unit", metric.Unit);
        Assert.Equal(100.0, metric.Score);
        Assert.Equal("source", metric.Source);
        Assert.Equal("note", metric.Note);

        // Default constructor
        var defaultMetric = new ContextMetricModel();
        Assert.NotNull(defaultMetric);
    }

    [Fact]
    public void ResolvedLocationModel_Constructor_InitializesCorrectly()
    {
        // Parameterized constructor
        var location = new ResolvedLocationModel(
            "query", "address", 52.0, 4.0, 100, 200, "GM", "Municipality", "WK", "District", "BU", "Neighborhood", "1234AB");

        Assert.Equal("query", location.Query);
        Assert.Equal("address", location.DisplayAddress);
        Assert.Equal(52.0, location.Latitude);
        Assert.Equal(4.0, location.Longitude);
        Assert.Equal(100, location.RdX);
        Assert.Equal(200, location.RdY);
        Assert.Equal("GM", location.MunicipalityCode);
        Assert.Equal("Municipality", location.MunicipalityName);
        Assert.Equal("WK", location.DistrictCode);
        Assert.Equal("District", location.DistrictName);
        Assert.Equal("BU", location.NeighborhoodCode);
        Assert.Equal("Neighborhood", location.NeighborhoodName);
        Assert.Equal("1234AB", location.PostalCode);

        // Default constructor
        var defaultLocation = new ResolvedLocationModel { Query = "test", DisplayAddress = "test" };
        Assert.NotNull(defaultLocation);
    }

    [Fact]
    public void SourceAttributionModel_Constructor_InitializesCorrectly()
    {
        var now = DateTimeOffset.UtcNow;
        // Parameterized constructor
        var source = new SourceAttributionModel("source", "url", "license", now);

        Assert.Equal("source", source.Source);
        Assert.Equal("url", source.Url);
        Assert.Equal("license", source.License);
        Assert.Equal(now, source.RetrievedAtUtc);

        // Default constructor
        var defaultSource = new SourceAttributionModel();
        Assert.NotNull(defaultSource);
    }

    [Fact]
    public void ContextReportModel_Constructor_InitializesCorrectly()
    {
        var location = new ResolvedLocationModel { Query = "test", DisplayAddress = "test" };
        var metric = new ContextMetricModel();
        var source = new SourceAttributionModel();

        // Parameterized constructor
        var report = new ContextReportModel(
            location,
            new List<ContextMetricModel> { metric }, // Social
            new List<ContextMetricModel>(),
            new List<ContextMetricModel>(),
            new List<ContextMetricModel>(),
            new List<ContextMetricModel>(),
            new List<ContextMetricModel>(),
            new List<ContextMetricModel>(),
            85.0,
            new Dictionary<string, double> { ["Social"] = 85 },
            new List<SourceAttributionModel> { source },
            new List<string> { "warning" }
        );

        Assert.Same(location, report.Location);
        Assert.Single(report.SocialMetrics);
        Assert.Empty(report.CrimeMetrics);
        Assert.Empty(report.DemographicsMetrics);
        Assert.Empty(report.HousingMetrics);
        Assert.Empty(report.MobilityMetrics);
        Assert.Empty(report.AmenityMetrics);
        Assert.Empty(report.EnvironmentMetrics);
        Assert.Equal(85.0, report.CompositeScore);
        Assert.Single(report.CategoryScores);
        Assert.Single(report.Sources);
        Assert.Single(report.Warnings);

        // Default constructor
        var defaultReport = new ContextReportModel();
        Assert.NotNull(defaultReport);
    }

    [Fact]
    public void EstimateWozValue_ReturnsCorrectValue()
    {
        var timeProvider = TimeProvider.System;
        var metric = new ContextMetricModel { Key = "average_woz", Value = 450 };
        var report = new ContextReportModel
        {
            SocialMetrics = new List<ContextMetricModel> { metric }
        };

        var (value, referenceDate, source) = report.EstimateWozValue(timeProvider);

        Assert.Equal(450000, value);
        Assert.Equal("CBS Neighborhood Average", source);
        Assert.NotNull(referenceDate);
        Assert.Equal(timeProvider.GetUtcNow().Year - 1, referenceDate.Value.Year);
    }

    [Fact]
    public void EstimateWozValue_ReturnsNull_WhenMetricMissing()
    {
        var timeProvider = TimeProvider.System;
        var report = new ContextReportModel
        {
            SocialMetrics = new List<ContextMetricModel>()
        };

        var (value, referenceDate, source) = report.EstimateWozValue(timeProvider);

        Assert.Null(value);
        Assert.Null(referenceDate);
        Assert.Null(source);
    }
}
