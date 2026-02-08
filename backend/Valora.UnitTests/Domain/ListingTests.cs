using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.Domain;

public class ListingTests
{
    [Fact]
    public void Merge_ShouldOverwritePrice_EvenIfNull()
    {
        // Arrange
        var target = new Listing
        {
            FundaId = "1",
            Address = "Old",
            Price = 500000
        };

        var source = new Listing
        {
            FundaId = "1",
            Address = "New",
            Price = null // Price removed/hidden
        };

        // Act
        target.Merge(source);

        // Assert
        Assert.Null(target.Price);
    }

    [Fact]
    public void Merge_ShouldOverwriteHasGarage_EvenIfFalse()
    {
        // Arrange
        var target = new Listing
        {
            FundaId = "1",
            Address = "Old",
            HasGarage = true
        };

        var source = new Listing
        {
            FundaId = "1",
            Address = "New",
            HasGarage = false
        };

        // Act
        target.Merge(source);

        // Assert
        Assert.False(target.HasGarage);
    }
}
