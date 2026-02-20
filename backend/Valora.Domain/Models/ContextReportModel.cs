using Valora.Domain.Common;

namespace Valora.Domain.Models;

public sealed record ContextReportModel
{
    public ResolvedLocationModel Location { get; init; } = null!;
    public List<ContextMetricModel> SocialMetrics { get; init; } = [];
    public List<ContextMetricModel> CrimeMetrics { get; init; } = [];
    public List<ContextMetricModel> DemographicsMetrics { get; init; } = [];
    public List<ContextMetricModel> HousingMetrics { get; init; } = [];
    public List<ContextMetricModel> MobilityMetrics { get; init; } = [];
    public List<ContextMetricModel> AmenityMetrics { get; init; } = [];
    public List<ContextMetricModel> EnvironmentMetrics { get; init; } = [];
    public double CompositeScore { get; init; }
    public Dictionary<string, double> CategoryScores { get; init; } = [];
    public List<SourceAttributionModel> Sources { get; init; } = [];
    public List<string> Warnings { get; init; } = [];

    public ContextReportModel() { }

    public ContextReportModel(
        ResolvedLocationModel location,
        List<ContextMetricModel> socialMetrics,
        List<ContextMetricModel> crimeMetrics,
        List<ContextMetricModel> demographicsMetrics,
        List<ContextMetricModel> housingMetrics,
        List<ContextMetricModel> mobilityMetrics,
        List<ContextMetricModel> amenityMetrics,
        List<ContextMetricModel> environmentMetrics,
        double compositeScore,
        Dictionary<string, double> categoryScores,
        List<SourceAttributionModel> sources,
        List<string> warnings)
    {
        Location = location;
        SocialMetrics = socialMetrics;
        CrimeMetrics = crimeMetrics;
        DemographicsMetrics = demographicsMetrics;
        HousingMetrics = housingMetrics;
        MobilityMetrics = mobilityMetrics;
        AmenityMetrics = amenityMetrics;
        EnvironmentMetrics = environmentMetrics;
        CompositeScore = compositeScore;
        CategoryScores = categoryScores;
        Sources = sources;
        Warnings = warnings;
    }

    public (int? Value, DateTime? ReferenceDate, string? Source) EstimateWozValue(TimeProvider timeProvider)
    {
        var avgWozMetric = SocialMetrics.FirstOrDefault(m => m.Key == "average_woz");
        if (avgWozMetric?.Value.HasValue == true)
        {
            // Value is in k€ (e.g. 450), convert to absolute value
            var value = (int)(avgWozMetric.Value.Value * 1000);
            var source = DataSources.CbsStatLine;
            // CBS data is typically from the previous year
            var now = timeProvider.GetUtcNow();
            var referenceDate = new DateTime(now.Year - 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (value, referenceDate, source);
        }

        return (null, null, null);
    }
}
