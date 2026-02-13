using Valora.Domain.Entities;
using Valora.Infrastructure.Persistence.Extensions;
using Xunit;

namespace Valora.UnitTests.Infrastructure.Persistence.Extensions;

public class ListingQueryExtensionsTests
{
    [Fact]
    public void WhereActive_ReturnsOnlyActiveListings()
    {
        // Arrange
        var listings = new List<Listing>
        {
            new Listing { Id = Guid.NewGuid(), FundaId = "1", Address = "A", Status = "Verkocht", IsSoldOrRented = false },
            new Listing { Id = Guid.NewGuid(), FundaId = "2", Address = "B", Status = "Ingetrokken", IsSoldOrRented = false },
            new Listing { Id = Guid.NewGuid(), FundaId = "3", Address = "C", Status = "Beschikbaar", IsSoldOrRented = true },
            new Listing { Id = Guid.NewGuid(), FundaId = "4", Address = "D", Status = "Beschikbaar", IsSoldOrRented = false }, // Active
            new Listing { Id = Guid.NewGuid(), FundaId = "5", Address = "E", Status = "Onder bod", IsSoldOrRented = false },   // Active
            new Listing { Id = Guid.NewGuid(), FundaId = "6", Address = "F", Status = "Onder optie", IsSoldOrRented = false }  // Active
        }.AsQueryable();

        // Act
        var result = listings.WhereActive().ToList();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Contains(result, l => l.Status == "Beschikbaar" && !l.IsSoldOrRented);
        Assert.Contains(result, l => l.Status == "Onder bod");
        Assert.Contains(result, l => l.Status == "Onder optie");

        Assert.DoesNotContain(result, l => l.Status == "Verkocht");
        Assert.DoesNotContain(result, l => l.Status == "Ingetrokken");
        Assert.DoesNotContain(result, l => l.IsSoldOrRented == true);
    }

    [Fact]
    public void WhereActive_ReturnsEmpty_WhenAllInactive()
    {
        // Arrange
        var listings = new List<Listing>
        {
            new Listing { FundaId = "1", Address = "A", Status = "Verkocht" },
            new Listing { FundaId = "2", Address = "B", Status = "Ingetrokken" },
            new Listing { FundaId = "3", Address = "C", IsSoldOrRented = true }
        }.AsQueryable();

        // Act
        var result = listings.WhereActive().ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void WhereActive_ReturnsAll_WhenAllActive()
    {
        // Arrange
        var listings = new List<Listing>
        {
            new Listing { FundaId = "1", Address = "A", Status = "Beschikbaar", IsSoldOrRented = false },
            new Listing { FundaId = "2", Address = "B", Status = "Onder bod", IsSoldOrRented = false }
        }.AsQueryable();

        // Act
        var result = listings.WhereActive().ToList();

        // Assert
        Assert.Equal(2, result.Count);
    }
}
