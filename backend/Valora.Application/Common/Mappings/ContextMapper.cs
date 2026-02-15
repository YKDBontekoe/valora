using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Application.Common.Mappings;

public static class ContextMapper
{
    public static NeighborhoodStatsDto ToDto(this CbsNeighborhoodStats entity)
    {
        return new NeighborhoodStatsDto(
            RegionCode: entity.RegionCode,
            RegionType: entity.RegionType ?? "Onbekend",
            Residents: entity.Residents,
            PopulationDensity: entity.PopulationDensity,
            AverageWozValueKeur: entity.AverageWozValueKeur,
            LowIncomeHouseholdsPercent: entity.LowIncomeHouseholdsPercent,
            Men: entity.Men,
            Women: entity.Women,
            Age0To15: entity.Age0To15,
            Age15To25: entity.Age15To25,
            Age25To45: entity.Age25To45,
            Age45To65: entity.Age45To65,
            Age65Plus: entity.Age65Plus,
            SingleHouseholds: entity.SingleHouseholds,
            HouseholdsWithoutChildren: entity.HouseholdsWithoutChildren,
            HouseholdsWithChildren: entity.HouseholdsWithChildren,
            AverageHouseholdSize: entity.AverageHouseholdSize,
            Urbanity: entity.Urbanity,
            AverageIncomePerRecipient: entity.AverageIncomePerRecipient,
            AverageIncomePerInhabitant: entity.AverageIncomePerInhabitant,
            EducationLow: entity.EducationLow,
            EducationMedium: entity.EducationMedium,
            EducationHigh: entity.EducationHigh,
            PercentageOwnerOccupied: entity.PercentageOwnerOccupied,
            PercentageRental: entity.PercentageRental,
            PercentageSocialHousing: entity.PercentageSocialHousing,
            PercentagePrivateRental: entity.PercentagePrivateRental,
            PercentagePre2000: entity.PercentagePre2000,
            PercentagePost2000: entity.PercentagePost2000,
            PercentageMultiFamily: entity.PercentageMultiFamily,
            CarsPerHousehold: entity.CarsPerHousehold,
            CarDensity: entity.CarDensity,
            TotalCars: entity.TotalCars,
            DistanceToGp: entity.DistanceToGp,
            DistanceToSupermarket: entity.DistanceToSupermarket,
            DistanceToDaycare: entity.DistanceToDaycare,
            DistanceToSchool: entity.DistanceToSchool,
            SchoolsWithin3km: entity.SchoolsWithin3km,
            RetrievedAtUtc: entity.RetrievedAtUtc);
    }

    public static CbsNeighborhoodStats ToEntity(this NeighborhoodStatsDto dto, string? datasetId, DateTimeOffset expiresAt)
    {
        return new CbsNeighborhoodStats
        {
            RegionCode = dto.RegionCode,
            DatasetId = datasetId,
            RegionType = dto.RegionType,
            Residents = dto.Residents,
            PopulationDensity = dto.PopulationDensity,
            AverageWozValueKeur = dto.AverageWozValueKeur,
            LowIncomeHouseholdsPercent = dto.LowIncomeHouseholdsPercent,
            Men = dto.Men,
            Women = dto.Women,
            Age0To15 = dto.Age0To15,
            Age15To25 = dto.Age15To25,
            Age25To45 = dto.Age25To45,
            Age45To65 = dto.Age45To65,
            Age65Plus = dto.Age65Plus,
            SingleHouseholds = dto.SingleHouseholds,
            HouseholdsWithoutChildren = dto.HouseholdsWithoutChildren,
            HouseholdsWithChildren = dto.HouseholdsWithChildren,
            AverageHouseholdSize = dto.AverageHouseholdSize,
            Urbanity = dto.Urbanity,
            AverageIncomePerRecipient = dto.AverageIncomePerRecipient,
            AverageIncomePerInhabitant = dto.AverageIncomePerInhabitant,
            EducationLow = dto.EducationLow,
            EducationMedium = dto.EducationMedium,
            EducationHigh = dto.EducationHigh,
            PercentageOwnerOccupied = dto.PercentageOwnerOccupied,
            PercentageRental = dto.PercentageRental,
            PercentageSocialHousing = dto.PercentageSocialHousing,
            PercentagePrivateRental = dto.PercentagePrivateRental,
            PercentagePre2000 = dto.PercentagePre2000,
            PercentagePost2000 = dto.PercentagePost2000,
            PercentageMultiFamily = dto.PercentageMultiFamily,
            CarsPerHousehold = dto.CarsPerHousehold,
            CarDensity = dto.CarDensity,
            TotalCars = dto.TotalCars,
            DistanceToGp = dto.DistanceToGp,
            DistanceToSupermarket = dto.DistanceToSupermarket,
            DistanceToDaycare = dto.DistanceToDaycare,
            DistanceToSchool = dto.DistanceToSchool,
            SchoolsWithin3km = dto.SchoolsWithin3km,
            RetrievedAtUtc = dto.RetrievedAtUtc,
            ExpiresAtUtc = expiresAt
        };
    }

    public static CrimeStatsDto ToDto(this CbsCrimeStats entity)
    {
        return new CrimeStatsDto(
            TotalCrimesPer1000: entity.TotalCrimesPer1000,
            BurglaryPer1000: entity.BurglaryPer1000,
            ViolentCrimePer1000: entity.ViolentCrimePer1000,
            TheftPer1000: entity.TheftPer1000,
            VandalismPer1000: entity.VandalismPer1000,
            YearOverYearChangePercent: entity.YearOverYearChangePercent,
            RetrievedAtUtc: entity.RetrievedAtUtc);
    }

    public static CbsCrimeStats ToEntity(this CrimeStatsDto dto, string? datasetId, DateTimeOffset expiresAt, string regionCode)
    {
        return new CbsCrimeStats
        {
            RegionCode = regionCode,
            DatasetId = datasetId,
            TotalCrimesPer1000 = dto.TotalCrimesPer1000,
            BurglaryPer1000 = dto.BurglaryPer1000,
            ViolentCrimePer1000 = dto.ViolentCrimePer1000,
            TheftPer1000 = dto.TheftPer1000,
            VandalismPer1000 = dto.VandalismPer1000,
            YearOverYearChangePercent = dto.YearOverYearChangePercent,
            RetrievedAtUtc = dto.RetrievedAtUtc,
            ExpiresAtUtc = expiresAt
        };
    }

    public static AirQualitySnapshotDto ToDto(this AirQualitySnapshot entity)
    {
        return new AirQualitySnapshotDto(
            StationId: entity.StationId,
            StationName: entity.StationName ?? "Onbekend",
            StationDistanceMeters: entity.StationDistanceMeters,
            Pm25: entity.Pm25,
            MeasuredAtUtc: entity.MeasuredAtUtc,
            RetrievedAtUtc: entity.RetrievedAtUtc,
            Pm10: entity.Pm10,
            No2: entity.No2,
            O3: entity.O3);
    }

    public static AirQualitySnapshot ToEntity(this AirQualitySnapshotDto dto, DateTimeOffset expiresAt)
    {
        return new AirQualitySnapshot
        {
            StationId = dto.StationId,
            StationName = dto.StationName,
            StationDistanceMeters = dto.StationDistanceMeters,
            Pm25 = dto.Pm25,
            Pm10 = dto.Pm10,
            No2 = dto.No2,
            O3 = dto.O3,
            MeasuredAtUtc = dto.MeasuredAtUtc,
            RetrievedAtUtc = dto.RetrievedAtUtc,
            ExpiresAtUtc = expiresAt
        };
    }

    public static AmenityStatsDto ToDto(this AmenityCache entity)
    {
        return new AmenityStatsDto(
            SchoolCount: entity.SchoolCount,
            SupermarketCount: entity.SupermarketCount,
            ParkCount: entity.ParkCount,
            HealthcareCount: entity.HealthcareCount,
            TransitStopCount: entity.TransitStopCount,
            NearestAmenityDistanceMeters: entity.NearestAmenityDistanceMeters,
            DiversityScore: entity.DiversityScore,
            RetrievedAtUtc: entity.RetrievedAtUtc,
            ChargingStationCount: entity.ChargingStationCount);
    }

    public static AmenityCache ToEntity(this AmenityStatsDto dto, string locationKey, double lat, double lon, int radius, DateTimeOffset expiresAt)
    {
        return new AmenityCache
        {
            LocationKey = locationKey,
            Latitude = lat,
            Longitude = lon,
            RadiusMeters = radius,
            SchoolCount = dto.SchoolCount,
            SupermarketCount = dto.SupermarketCount,
            ParkCount = dto.ParkCount,
            HealthcareCount = dto.HealthcareCount,
            TransitStopCount = dto.TransitStopCount,
            ChargingStationCount = dto.ChargingStationCount,
            NearestAmenityDistanceMeters = dto.NearestAmenityDistanceMeters,
            DiversityScore = dto.DiversityScore,
            RetrievedAtUtc = dto.RetrievedAtUtc,
            ExpiresAtUtc = expiresAt
        };
    }
}
