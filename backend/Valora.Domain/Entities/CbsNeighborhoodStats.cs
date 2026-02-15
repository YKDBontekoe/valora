using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class CbsNeighborhoodStats : BaseEntity
{
    public required string RegionCode { get; set; }
    public string? DatasetId { get; set; }
    public string? RegionType { get; set; }
    public int? Residents { get; set; }
    public int? PopulationDensity { get; set; }
    public double? AverageWozValueKeur { get; set; }
    public double? LowIncomeHouseholdsPercent { get; set; }
    public int? Men { get; set; }
    public int? Women { get; set; }
    public int? Age0To15 { get; set; }
    public int? Age15To25 { get; set; }
    public int? Age25To45 { get; set; }
    public int? Age45To65 { get; set; }
    public int? Age65Plus { get; set; }
    public int? SingleHouseholds { get; set; }
    public int? HouseholdsWithoutChildren { get; set; }
    public int? HouseholdsWithChildren { get; set; }
    public double? AverageHouseholdSize { get; set; }
    public string? Urbanity { get; set; }
    public double? AverageIncomePerRecipient { get; set; }
    public double? AverageIncomePerInhabitant { get; set; }
    public int? EducationLow { get; set; }
    public int? EducationMedium { get; set; }
    public int? EducationHigh { get; set; }
    public int? PercentageOwnerOccupied { get; set; }
    public int? PercentageRental { get; set; }
    public int? PercentageSocialHousing { get; set; }
    public int? PercentagePrivateRental { get; set; }
    public int? PercentagePre2000 { get; set; }
    public int? PercentagePost2000 { get; set; }
    public int? PercentageMultiFamily { get; set; }
    public double? CarsPerHousehold { get; set; }
    public int? CarDensity { get; set; }
    public int? TotalCars { get; set; }
    public double? DistanceToGp { get; set; }
    public double? DistanceToSupermarket { get; set; }
    public double? DistanceToDaycare { get; set; }
    public double? DistanceToSchool { get; set; }
    public double? SchoolsWithin3km { get; set; }
    public DateTimeOffset RetrievedAtUtc { get; set; }
    public DateTimeOffset ExpiresAtUtc { get; set; }
}
