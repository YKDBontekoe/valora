using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;

namespace Valora.UnitTests.Persistence;

public class ValoraDbContextTests
{
    [Fact]
    public void Model_Configuration_AppliesSecurityConstraints()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new ValoraDbContext(options);

        // Act
        var model = context.Model;
        var listingEntity = model.FindEntityType(typeof(Listing));

        // Assert
        Assert.NotNull(listingEntity);

        // Check Description MaxLength
        var descriptionProp = listingEntity.FindProperty(nameof(Listing.Description));
        Assert.NotNull(descriptionProp);
        Assert.Equal(10000, descriptionProp.GetMaxLength());

        // Check URL MaxLengths
        var videoUrlProp = listingEntity.FindProperty(nameof(Listing.VideoUrl));
        Assert.NotNull(videoUrlProp);
        Assert.Equal(500, videoUrlProp.GetMaxLength());

        var virtualTourUrlProp = listingEntity.FindProperty(nameof(Listing.VirtualTourUrl));
        Assert.NotNull(virtualTourUrlProp);
        Assert.Equal(500, virtualTourUrlProp.GetMaxLength());

        var brochureUrlProp = listingEntity.FindProperty(nameof(Listing.BrochureUrl));
        Assert.NotNull(brochureUrlProp);
        Assert.Equal(500, brochureUrlProp.GetMaxLength());

        var brokerLogoUrlProp = listingEntity.FindProperty(nameof(Listing.BrokerLogoUrl));
        Assert.NotNull(brokerLogoUrlProp);
        Assert.Equal(500, brokerLogoUrlProp.GetMaxLength());
    }
}
