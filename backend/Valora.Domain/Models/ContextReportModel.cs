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
            var source = "CBS Neighborhood Average";
            // CBS data is typically from the previous year
            var now = timeProvider.GetUtcNow();
            var referenceDate = new DateTime(now.Year - 1, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return (value, referenceDate, source);
        }

        return (null, null, null);
    }
}

public sealed record ResolvedLocationModel
{
    public string Query { get; init; } = null!;
    public string DisplayAddress { get; init; } = null!;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double? RdX { get; init; }
    public double? RdY { get; init; }
    public string? MunicipalityCode { get; init; }
    public string? MunicipalityName { get; init; }
    public string? DistrictCode { get; init; }
    public string? DistrictName { get; init; }
    public string? NeighborhoodCode { get; init; }
    public string? NeighborhoodName { get; init; }
    public string? PostalCode { get; init; }

    public ResolvedLocationModel() { }

    public ResolvedLocationModel(
        string query,
        string displayAddress,
        double latitude,
        double longitude,
        double? rdX,
        double? rdY,
        string? municipalityCode,
        string? municipalityName,
        string? districtCode,
        string? districtName,
        string? neighborhoodCode,
        string? neighborhoodName,
        string? postalCode)
    {
        Query = query;
        DisplayAddress = displayAddress;
        Latitude = latitude;
        Longitude = longitude;
        RdX = rdX;
        RdY = rdY;
        MunicipalityCode = municipalityCode;
        MunicipalityName = municipalityName;
        DistrictCode = districtCode;
        DistrictName = districtName;
        NeighborhoodCode = neighborhoodCode;
        NeighborhoodName = neighborhoodName;
        PostalCode = postalCode;
    }
}

public sealed record ContextMetricModel
{
    public string Key { get; init; } = null!;
    public string Label { get; init; } = null!;
    public double? Value { get; init; }
    public string? Unit { get; init; }
    public double? Score { get; init; }
    public string Source { get; init; } = null!;
    public string? Note { get; init; }

    public ContextMetricModel() { }

    public ContextMetricModel(
        string key,
        string label,
        double? value,
        string? unit,
        double? score,
        string source,
        string? note = null)
    {
        Key = key;
        Label = label;
        Value = value;
        Unit = unit;
        Score = score;
        Source = source;
        Note = note;
    }
}

public sealed record SourceAttributionModel
{
    public string Source { get; init; } = null!;
    public string Url { get; init; } = null!;
    public string License { get; init; } = null!;
    public DateTimeOffset RetrievedAtUtc { get; init; }

    public SourceAttributionModel() { }

    public SourceAttributionModel(
        string source,
        string url,
        string license,
        DateTimeOffset retrievedAtUtc)
    {
        Source = source;
        Url = url;
        License = license;
        RetrievedAtUtc = retrievedAtUtc;
    }
}
