using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public sealed record ContextReportRequestDto(
    [property: Required] [property: MaxLength(200)] string Input,
    [property: Range(100, 5000)] int RadiusMeters = 1000);

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

public sealed record CrimeStatsDto(
    int? TotalCrimesPer1000,
    int? BurglaryPer1000,
    int? ViolentCrimePer1000,
    int? TheftPer1000,
    int? VandalismPer1000,
    double? YearOverYearChangePercent,
    DateTimeOffset RetrievedAtUtc);

public sealed record DemographicsDto(
    int? PercentAge0To14,
    int? PercentAge15To24,
    int? PercentAge25To44,
    int? PercentAge45To64,
    int? PercentAge65Plus,
    double? AverageHouseholdSize,
    int? PercentOwnerOccupied,
    int? PercentSingleHouseholds,
    int? PercentFamilyHouseholds,
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
    IReadOnlyList<ContextMetricDto> CrimeMetrics,
    IReadOnlyList<ContextMetricDto> DemographicsMetrics,
    IReadOnlyList<ContextMetricDto> AmenityMetrics,
    IReadOnlyList<ContextMetricDto> EnvironmentMetrics,
    double CompositeScore,
    IReadOnlyDictionary<string, double> CategoryScores,
    IReadOnlyList<SourceAttributionDto> Sources,
    IReadOnlyList<string> Warnings);

