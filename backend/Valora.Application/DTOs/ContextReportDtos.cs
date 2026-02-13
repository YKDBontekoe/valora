using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public sealed record ContextReportRequestDto(
    [property: Required(ErrorMessage = "Input address is required.")]
    [property: StringLength(200, MinimumLength = 3, ErrorMessage = "Input must be between 3 and 200 characters.")]
    string Input,
    [property: Range(100, 5000, ErrorMessage = "Radius must be between 100 and 5000 meters.")]
    int RadiusMeters = 1000);

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
    // New Fields
    int? Men,
    int? Women,
    int? Age0To15,
    int? Age15To25,
    int? Age25To45,
    int? Age45To65,
    int? Age65Plus,
    int? SingleHouseholds,
    int? HouseholdsWithoutChildren,
    int? HouseholdsWithChildren,
    double? AverageHouseholdSize,
    string? Urbanity, // MateVanStedelijkheid
    double? AverageIncomePerRecipient, // x1000
    double? AverageIncomePerInhabitant, // x1000
    int? EducationLow, // BasisonderwijsVmboMbo1
    int? EducationMedium, // HavoVwoMbo24
    int? EducationHigh, // HboWo
    // Phase 2: Housing
    int? PercentageOwnerOccupied, // Koopwoningen_41
    int? PercentageRental, // HuurwoningenTotaal_42
    int? PercentageSocialHousing, // InBezitWoningcorporatie_43
    int? PercentagePrivateRental, // InBezitOverigeVerhuurders_44
    int? PercentagePre2000, // BouwjaarVoor2000_46
    int? PercentagePost2000, // BouwjaarVanaf2000_47
    int? PercentageMultiFamily, // PercentageMeergezinswoning_38
    // Phase 2: Mobility
    double? CarsPerHousehold, // PersonenautoSPerHuishouden_112
    int? CarDensity, // PersonenautoSNaarOppervlakte_113
    int? TotalCars, // PersonenautoSTotaal_109
    // Phase 2: Proximity
    double? DistanceToGp, // AfstandTotHuisartsenpraktijk_115
    double? DistanceToSupermarket, // AfstandTotGroteSupermarkt_116
    double? DistanceToDaycare, // AfstandTotKinderdagverblijf_117
    double? DistanceToSchool, // AfstandTotSchool_118
    double? SchoolsWithin3km, // ScholenBinnen3Km_119
    DateTimeOffset RetrievedAtUtc);

public sealed record CrimeStatsDto(
    int? TotalCrimesPer1000,
    int? BurglaryPer1000,
    int? ViolentCrimePer1000,
    int? TheftPer1000,
    int? VandalismPer1000,
    double? YearOverYearChangePercent,
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

public sealed record ContextSourceData(
    NeighborhoodStatsDto? NeighborhoodStats,
    CrimeStatsDto? CrimeStats,
    AmenityStatsDto? AmenityStats,
    AirQualitySnapshotDto? AirQualitySnapshot,
    IReadOnlyList<SourceAttributionDto> Sources,
    IReadOnlyList<string> Warnings);

public sealed record ContextReportDto(
    ResolvedLocationDto Location,
    IReadOnlyList<ContextMetricDto> SocialMetrics,
    IReadOnlyList<ContextMetricDto> CrimeMetrics,
    IReadOnlyList<ContextMetricDto> DemographicsMetrics,
    IReadOnlyList<ContextMetricDto> HousingMetrics, // Phase 2
    IReadOnlyList<ContextMetricDto> MobilityMetrics, // Phase 2
    IReadOnlyList<ContextMetricDto> AmenityMetrics,
    IReadOnlyList<ContextMetricDto> EnvironmentMetrics,
    double CompositeScore,
    IReadOnlyDictionary<string, double> CategoryScores,
    IReadOnlyList<SourceAttributionDto> Sources,
    IReadOnlyList<string> Warnings);
