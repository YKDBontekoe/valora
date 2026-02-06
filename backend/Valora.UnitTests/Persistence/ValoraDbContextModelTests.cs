using Microsoft.EntityFrameworkCore;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.UnitTests.Persistence;

public class ValoraDbContextModelTests
{
    [Fact]
    public void Listing_HasCorrectMaxLengthConstraints()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: "ValoraModelTestDb")
            .Options;

        using var context = new ValoraDbContext(options);

        // Act
        var entityType = context.Model.FindEntityType(typeof(Listing));
        Assert.NotNull(entityType);

        // Assert
        AssertMaxLength(entityType, nameof(Listing.Description), 10000);
        AssertMaxLength(entityType, nameof(Listing.VideoUrl), 500);
        AssertMaxLength(entityType, nameof(Listing.VirtualTourUrl), 500);
        AssertMaxLength(entityType, nameof(Listing.BrochureUrl), 500);
        AssertMaxLength(entityType, nameof(Listing.BrokerLogoUrl), 500);

        // Check previously existing ones too just in case
        AssertMaxLength(entityType, nameof(Listing.Address), 200);
        AssertMaxLength(entityType, nameof(Listing.City), 100);
    }

    private void AssertMaxLength(Microsoft.EntityFrameworkCore.Metadata.IEntityType entityType, string propertyName, int expectedLength)
    {
        var property = entityType.FindProperty(propertyName);
        Assert.NotNull(property);
        var maxLength = property.GetMaxLength();
        Assert.Equal(expectedLength, maxLength);
    }
}
