using System.ComponentModel.DataAnnotations;
using Valora.Application.Common.Validation;

namespace Valora.Application.DTOs;

public sealed record ContextReportRequestDto(
    [property: Required(ErrorMessage = "Input address is required.")]
    [property: StringLength(200, MinimumLength = 3, ErrorMessage = "Input must be between 3 and 200 characters.")]
    string Input,
    [property: Range(100, 5000, ErrorMessage = "Radius must be between 100 and 5000 meters.")]
    int RadiusMeters = 1000);

public sealed record ResolvedLocationDto(
    [property: StringLength(200)] string Query,
    [property: StringLength(200)] string DisplayAddress,
    double Latitude,
    double Longitude,
    double? RdX,
    double? RdY,
    [property: StringLength(50)] string? MunicipalityCode,
    [property: StringLength(100)] string? MunicipalityName,
    [property: StringLength(50)] string? DistrictCode,
    [property: StringLength(100)] string? DistrictName,
    [property: StringLength(50)] string? NeighborhoodCode,
    [property: StringLength(100)] string? NeighborhoodName,
    [property: StringLength(20)] string? PostalCode);

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
    DateTimeOffset RetrievedAtUtc,
    int ChargingStationCount = 0);

public sealed record AirQualitySnapshotDto(
    string StationId,
    string StationName,
    double StationDistanceMeters,
    double? Pm25,
    DateTimeOffset? MeasuredAtUtc,
    DateTimeOffset RetrievedAtUtc,
    double? Pm10 = null,
    double? No2 = null,
    double? O3 = null);

public sealed record ContextMetricDto(
    [property: Required] [property: StringLength(100)] string Key,
    [property: Required] [property: StringLength(200)] string Label,
    double? Value,
    [property: StringLength(50)] string? Unit,
    double? Score,
    [property: StringLength(100)] string Source,
    [property: StringLength(500)] string? Note = null);

public sealed record SourceAttributionDto(
    [property: StringLength(200)] string Source,
    [property: StringLength(500)] string Url,
    [property: StringLength(500)] string License,
    DateTimeOffset RetrievedAtUtc);

public sealed record ContextSourceData(
    NeighborhoodStatsDto? NeighborhoodStats,
    CrimeStatsDto? CrimeStats,
    AmenityStatsDto? AmenityStats,
    AirQualitySnapshotDto? AirQualitySnapshot,
    IReadOnlyList<SourceAttributionDto> Sources,
    IReadOnlyList<string> Warnings);

public sealed record ContextReportDto(
    [property: Required] ResolvedLocationDto Location,
    [property: MaxCollectionSize(50)] IReadOnlyList<ContextMetricDto> SocialMetrics,
    [property: MaxCollectionSize(50)] IReadOnlyList<ContextMetricDto> CrimeMetrics,
    [property: MaxCollectionSize(50)] IReadOnlyList<ContextMetricDto> DemographicsMetrics,
    [property: MaxCollectionSize(50)] IReadOnlyList<ContextMetricDto> HousingMetrics, // Phase 2
    [property: MaxCollectionSize(50)] IReadOnlyList<ContextMetricDto> MobilityMetrics, // Phase 2
    [property: MaxCollectionSize(50)] IReadOnlyList<ContextMetricDto> AmenityMetrics,
    [property: MaxCollectionSize(50)] IReadOnlyList<ContextMetricDto> EnvironmentMetrics,
    double CompositeScore,
    [property: MaxCollectionSize(20)] IReadOnlyDictionary<string, double> CategoryScores,
    [property: MaxCollectionSize(20)] IReadOnlyList<SourceAttributionDto> Sources,
    [property: MaxCollectionSize(50)] IReadOnlyList<string> Warnings)
{
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
