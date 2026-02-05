using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.Domain;

public class ListingTests
{
    [Fact]
    public void UpdateFrom_ShouldUpdateProperties_WhenSourceHasValue()
    {
        // Arrange
        var target = new Listing { FundaId = "1", Address = "Old Address" };
        var source = new Listing
        {
            FundaId = "1",
            Address = "Old Address",
            Bedrooms = 3,
            LivingAreaM2 = 100,
            Price = 500000,
            Status = "Sold",
            BrokerOfficeId = 123,
            BrokerPhone = "06123",
            BrokerLogoUrl = "logo.png",
            BrokerAssociationCode = "NVM",
            FiberAvailable = true,
            PublicationDate = new DateTime(2023, 1, 1),
            Labels = ["New Label"],
            PostalCode = "1234AB",
            AgentName = "Agent",
            ImageUrl = "new.jpg",
            Description = "Desc",
            EnergyLabel = "A",
            YearBuilt = 2020,
            Latitude = 52.0,
            Longitude = 4.0,
            VideoUrl = "vid",
            VirtualTourUrl = "tour",
            BrochureUrl = "pdf",
            ViewCount = 10,
            SaveCount = 5,
            NeighborhoodPopulation = 1000,
            NeighborhoodAvgPriceM2 = 5000,
            RoofType = "Flat",
            NumberOfFloors = 2,
            ConstructionPeriod = "2020s",
            CVBoilerBrand = "Brand",
            CVBoilerYear = 2021,
            OwnershipType = "Full",
            CadastralDesignation = "CAD1",
            VVEContribution = 100,
            HeatingType = "Gas",
            InsulationType = "Full",
            GardenOrientation = "South",
            HasGarage = true,
            ParkingType = "Public",
            VolumeM3 = 300,
            BalconyM2 = 10,
            GardenM2 = 50,
            ExternalStorageM2 = 5
        };

        // Act
        target.UpdateFrom(source);

        // Assert
        Assert.Equal(3, target.Bedrooms);
        Assert.Equal(100, target.LivingAreaM2);
        Assert.Equal(500000, target.Price);
        Assert.Equal("Sold", target.Status);
        Assert.Equal(123, target.BrokerOfficeId);
        Assert.Equal("06123", target.BrokerPhone);
        Assert.Equal("logo.png", target.BrokerLogoUrl);
        Assert.Equal("NVM", target.BrokerAssociationCode);
        Assert.True(target.FiberAvailable);
        Assert.Equal(new DateTime(2023, 1, 1), target.PublicationDate);
        Assert.Single(target.Labels);
        Assert.Equal("1234AB", target.PostalCode);
        Assert.Equal("Agent", target.AgentName);
        Assert.Equal("new.jpg", target.ImageUrl);
        Assert.Equal("Desc", target.Description);
        Assert.Equal("A", target.EnergyLabel);
        Assert.Equal(2020, target.YearBuilt);
        Assert.Equal(52.0, target.Latitude);
        Assert.Equal(4.0, target.Longitude);
        Assert.Equal("vid", target.VideoUrl);
        Assert.Equal("tour", target.VirtualTourUrl);
        Assert.Equal("pdf", target.BrochureUrl);
        Assert.Equal(10, target.ViewCount);
        Assert.Equal(5, target.SaveCount);
        Assert.Equal(1000, target.NeighborhoodPopulation);
        Assert.Equal(5000, target.NeighborhoodAvgPriceM2);
        Assert.Equal("Flat", target.RoofType);
        Assert.Equal(2, target.NumberOfFloors);
        Assert.Equal("2020s", target.ConstructionPeriod);
        Assert.Equal("Brand", target.CVBoilerBrand);
        Assert.Equal(2021, target.CVBoilerYear);
        Assert.Equal("Full", target.OwnershipType);
        Assert.Equal("CAD1", target.CadastralDesignation);
        Assert.Equal(100, target.VVEContribution);
        Assert.Equal("Gas", target.HeatingType);
        Assert.Equal("Full", target.InsulationType);
        Assert.Equal("South", target.GardenOrientation);
        Assert.True(target.HasGarage);
        Assert.Equal("Public", target.ParkingType);
        Assert.Equal(300, target.VolumeM3);
        Assert.Equal(10, target.BalconyM2);
        Assert.Equal(50, target.GardenM2);
        Assert.Equal(5, target.ExternalStorageM2);
    }

    [Fact]
    public void UpdateFrom_ShouldNotOverwrite_WhenSourceIsNull()
    {
        // Arrange
        var target = new Listing
        {
            FundaId = "1",
            Address = "Old",
            Bedrooms = 3,
            Price = 100000,
            Labels = ["Old"]
        };
        var source = new Listing
        {
            FundaId = "1",
            Address = "Old",
            // All other fields null/default
        };

        // Act
        target.UpdateFrom(source);

        // Assert
        Assert.Equal(3, target.Bedrooms);
        Assert.Equal(100000, target.Price);
        Assert.Single(target.Labels);
    }

    [Fact]
    public void UpdateFrom_ShouldHandleCollections_WhenSourceHasItems()
    {
        // Arrange
        var target = new Listing { FundaId = "1", Address = "A", ImageUrls = ["old1.jpg"] };
        var source = new Listing { FundaId = "1", Address = "A", ImageUrls = ["new1.jpg", "new2.jpg"] };

        // Act
        target.UpdateFrom(source);

        // Assert
        Assert.Equal(2, target.ImageUrls.Count);
        Assert.Contains("new1.jpg", target.ImageUrls);
        Assert.DoesNotContain("old1.jpg", target.ImageUrls);
    }

    [Fact]
    public void UpdateFrom_ShouldHandleOpenHouseDates()
    {
        // Arrange
        var target = new Listing { FundaId = "1", Address = "A", OpenHouseDates = [DateTime.Now.AddDays(-1)] };
        var source = new Listing { FundaId = "1", Address = "A", OpenHouseDates = [DateTime.Now.AddDays(1)] };

        // Act
        target.UpdateFrom(source);

        // Assert
        Assert.Single(target.OpenHouseDates);
        Assert.Equal(source.OpenHouseDates[0], target.OpenHouseDates[0]);
    }
}
