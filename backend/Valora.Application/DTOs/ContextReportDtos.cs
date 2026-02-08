namespace Valora.Application.DTOs;

public sealed record ContextReportRequestDto(string Input, int RadiusMeters = 1000);

public sealed record ResolvedLocationDto(
    string Query,
    string DisplayAddress,
    double Latitude,
    double Longitude,
    double? RdX,
    double? RdY,
    string? MunicipalityCode,
    string? MunicipalityName,
    string? DistrictCode,
    string? DistrictName,
    string? NeighborhoodCode,
    string? NeighborhoodName,
    string? PostalCode);

public sealed record NeighborhoodStatsDto(
    string RegionCode,
    string RegionType,
    int? Residents,
    int? PopulationDensity,
    double? AverageWozValueKeur,
    double? LowIncomeHouseholdsPercent,
    DateTimeOffset RetrievedAtUtc);

public sealed record AmenityStatsDto(
    int SchoolCount,
    int SupermarketCount,
    int ParkCount,
    int HealthcareCount,
    int TransitStopCount,
    double? NearestAmenityDistanceMeters,
    double DiversityScore,
    DateTimeOffset RetrievedAtUtc);

public sealed record AirQualitySnapshotDto(
    string StationId,
    string StationName,
    double StationDistanceMeters,
    double? Pm25,
    DateTimeOffset? MeasuredAtUtc,
    DateTimeOffset RetrievedAtUtc);

public sealed record ContextMetricDto(
    string Key,
    string Label,
    double? Value,
    string? Unit,
    double? Score,
    string Source,
    string? Note = null);

public sealed record SourceAttributionDto(
    string Source,
    string Url,
    string License,
    DateTimeOffset RetrievedAtUtc);

public sealed record ContextReportDto(
    ResolvedLocationDto Location,
    IReadOnlyList<ContextMetricDto> SocialMetrics,
    IReadOnlyList<ContextMetricDto> SafetyMetrics,
    IReadOnlyList<ContextMetricDto> AmenityMetrics,
    IReadOnlyList<ContextMetricDto> EnvironmentMetrics,
    double CompositeScore,
    IReadOnlyList<SourceAttributionDto> Sources,
    IReadOnlyList<string> Warnings);
