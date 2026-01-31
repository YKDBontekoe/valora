using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence;
using Xunit;

namespace Valora.UnitTests.Persistence;

public class ValoraDbContextTests
{
    [Fact]
    public void Listing_Configuration_ShouldHaveCorrectConstraints()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ValoraDbContext>()
            .UseInMemoryDatabase(databaseName: "ValoraDbTest")
            .Options;

        using var context = new ValoraDbContext(options);

        // Act
        var model = context.Model;
        var entity = model.FindEntityType(typeof(Listing));

        // Assert
        Assert.NotNull(entity);

        // Address: Required, MaxLength 255
        var addressProp = entity.FindProperty(nameof(Listing.Address));
        Assert.NotNull(addressProp);
        Assert.False(addressProp.IsNullable);
        Assert.Equal(255, addressProp.GetMaxLength());

        // City: MaxLength 100
        var cityProp = entity.FindProperty(nameof(Listing.City));
        Assert.NotNull(cityProp);
        Assert.Equal(100, cityProp.GetMaxLength());

        // PostalCode: MaxLength 20
        var postalCodeProp = entity.FindProperty(nameof(Listing.PostalCode));
        Assert.NotNull(postalCodeProp);
        Assert.Equal(20, postalCodeProp.GetMaxLength());

        // PropertyType: MaxLength 50
        var propertyTypeProp = entity.FindProperty(nameof(Listing.PropertyType));
        Assert.NotNull(propertyTypeProp);
        Assert.Equal(50, propertyTypeProp.GetMaxLength());

        // Status: MaxLength 50
        var statusProp = entity.FindProperty(nameof(Listing.Status));
        Assert.NotNull(statusProp);
        Assert.Equal(50, statusProp.GetMaxLength());

        // Url: MaxLength 2048
        var urlProp = entity.FindProperty(nameof(Listing.Url));
        Assert.NotNull(urlProp);
        Assert.Equal(2048, urlProp.GetMaxLength());

        // ImageUrl: MaxLength 2048
        var imageUrlProp = entity.FindProperty(nameof(Listing.ImageUrl));
        Assert.NotNull(imageUrlProp);
        Assert.Equal(2048, imageUrlProp.GetMaxLength());
    }
}
