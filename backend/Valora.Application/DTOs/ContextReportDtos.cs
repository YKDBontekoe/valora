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

public sealed record FoundationRiskDto(
    string RiskLevel,
    string SoilType,
    string? Description,
    DateTimeOffset RetrievedAtUtc);

public sealed record SolarPotentialDto(
    string Potential,
    double? RoofAreaM2,
    int? InstallablePanels,
    double? EstimatedGenerationKwh,
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
    IReadOnlyList<ContextMetricDto> HousingMetrics, // Phase 2
    IReadOnlyList<ContextMetricDto> MobilityMetrics, // Phase 2
    IReadOnlyList<ContextMetricDto> AmenityMetrics,
    IReadOnlyList<ContextMetricDto> EnvironmentMetrics,
    double CompositeScore,
    IReadOnlyDictionary<string, double> CategoryScores,
    IReadOnlyList<SourceAttributionDto> Sources,
    IReadOnlyList<string> Warnings);
