using Valora.Application.DTOs;
using Valora.Application.Enrichment.Builders;

namespace Valora.UnitTests.Services;

public class DemographicsMetricBuilderTests
{
    [Fact]
    public void Build_CalculatesCorrectPercentages()
    {
        // Arrange
        var cbs = new NeighborhoodStatsDto(
            RegionCode: "BU0363",
            RegionType: "Neighborhood",
            Residents: 1000,
            PopulationDensity: 5000,
            AverageWozValueKeur: 400,
            LowIncomeHouseholdsPercent: 10,
            Men: 500,
            Women: 500,
            Age0To15: 150, // 15%
            Age15To25: 100, // 10%
            Age25To45: 350, // 35%
            Age45To65: 250, // 25%
            Age65Plus: 150, // 15%
            SingleHouseholds: 400,
            HouseholdsWithoutChildren: 200,
            HouseholdsWithChildren: 400, // Total 1000 households
            AverageHouseholdSize: 2.1,
            Urbanity: "Urban",
            AverageIncomePerRecipient: 40,
            AverageIncomePerInhabitant: 35,
            EducationLow: 20,
            EducationMedium: 40,
            EducationHigh: 40,
            PercentageOwnerOccupied: 60,
            PercentageRental: 40,
            PercentageSocialHousing: 10,
            PercentagePrivateRental: 30,
            PercentagePre2000: 80,
            PercentagePost2000: 20,
            PercentageMultiFamily: 70,
            CarsPerHousehold: 1.0,
            CarDensity: 1000,
            TotalCars: 500,
            DistanceToGp: 0.5,
            DistanceToSupermarket: 0.3,
            DistanceToDaycare: 0.4,
            DistanceToSchool: 0.6,
            SchoolsWithin3km: 5.0,
            RetrievedAtUtc: DateTimeOffset.UtcNow);

        var warnings = new List<string>();

        // Act
        var metrics = DemographicsMetricBuilder.Build(cbs, warnings);

        // Assert
        Assert.Equal(15.0, metrics.Single(m => m.Key == "age_0_14").Value);
        Assert.Equal(10.0, metrics.Single(m => m.Key == "age_15_24").Value);
        Assert.Equal(40.0, metrics.Single(m => m.Key == "single_households").Value); // 400/1000
        Assert.Equal("%", metrics.First(m => m.Key == "age_0_14").Unit);
    }

    [Fact]
    public void Build_HandlesZeroResidents()
    {
        // Arrange
        var cbs = new NeighborhoodStatsDto(
            RegionCode: "BU0363",
            RegionType: "Neighborhood",
            Residents: 0,
            PopulationDensity: 0,
            AverageWozValueKeur: 400,
            LowIncomeHouseholdsPercent: 10,
            Men: 0,
            Women: 0,
            Age0To15: 0,
            Age15To25: 0,
            Age25To45: 0,
            Age45To65: 0,
            Age65Plus: 0,
            SingleHouseholds: 0,
            HouseholdsWithoutChildren: 0,
            HouseholdsWithChildren: 0,
            AverageHouseholdSize: 0,
            Urbanity: "Urban",
            AverageIncomePerRecipient: 0,
            AverageIncomePerInhabitant: 0,
            EducationLow: 0,
            EducationMedium: 0,
            EducationHigh: 0,
            PercentageOwnerOccupied: 0,
            PercentageRental: 0,
            PercentageSocialHousing: 0,
            PercentagePrivateRental: 0,
            PercentagePre2000: 0,
            PercentagePost2000: 0,
            PercentageMultiFamily: 0,
            CarsPerHousehold: 0,
            CarDensity: 0,
            TotalCars: 0,
            DistanceToGp: 0,
            DistanceToSupermarket: 0,
            DistanceToDaycare: 0,
            DistanceToSchool: 0,
            SchoolsWithin3km: 0,
            RetrievedAtUtc: DateTimeOffset.UtcNow);

        var warnings = new List<string>();

        // Act
        var metrics = DemographicsMetricBuilder.Build(cbs, warnings);

        // Assert
        Assert.Null(metrics.Single(m => m.Key == "age_0_14").Value);
        Assert.Null(metrics.Single(m => m.Key == "single_households").Value);
    }
}
