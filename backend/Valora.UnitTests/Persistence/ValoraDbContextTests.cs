using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.UnitTests.Persistence;

public class ValoraDbContextTests
{
    [Fact]
    public void OnModelCreating_AppliesListingConstraints()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: "ValoraDbContextTests_Constraints")
            .Options;

        using var context = new ValoraDbContext(options);

        // Act
        // Accessing the Model property triggers OnModelCreating
        var model = context.Model;
        var listingEntity = model.FindEntityType(typeof(Listing));

        // Assert
        Assert.NotNull(listingEntity);

        // Verify MaxLength constraints
        Assert.Equal(50, listingEntity.FindProperty(nameof(Listing.FundaId))?.GetMaxLength());
        Assert.Equal(200, listingEntity.FindProperty(nameof(Listing.Address))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.City))?.GetMaxLength());
        Assert.Equal(20, listingEntity.FindProperty(nameof(Listing.PostalCode))?.GetMaxLength());
        Assert.Equal(500, listingEntity.FindProperty(nameof(Listing.Url))?.GetMaxLength());
        Assert.Equal(500, listingEntity.FindProperty(nameof(Listing.ImageUrl))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.PropertyType))?.GetMaxLength());
        Assert.Equal(50, listingEntity.FindProperty(nameof(Listing.Status))?.GetMaxLength());
        Assert.Equal(20, listingEntity.FindProperty(nameof(Listing.EnergyLabel))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.OwnershipType))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.CadastralDesignation))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.HeatingType))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.InsulationType))?.GetMaxLength());
        Assert.Equal(50, listingEntity.FindProperty(nameof(Listing.GardenOrientation))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.ParkingType))?.GetMaxLength());
        Assert.Equal(200, listingEntity.FindProperty(nameof(Listing.AgentName))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.RoofType))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.ConstructionPeriod))?.GetMaxLength());
        Assert.Equal(100, listingEntity.FindProperty(nameof(Listing.CVBoilerBrand))?.GetMaxLength());
        Assert.Equal(50, listingEntity.FindProperty(nameof(Listing.BrokerPhone))?.GetMaxLength());
        Assert.Equal(20, listingEntity.FindProperty(nameof(Listing.BrokerAssociationCode))?.GetMaxLength());
    }
}
