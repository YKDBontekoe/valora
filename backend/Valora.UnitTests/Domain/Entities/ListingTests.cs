using Valora.Domain.Entities;

namespace Valora.UnitTests.Domain.Entities;

public class ListingTests
{
    [Fact]
    public void Merge_UpdatesTarget()
    {
        var target = new Listing { FundaId = "1", Address = "Test", Price = 100000 };
        var source = new Listing
        {
            FundaId = "1",
            Address = "Test",
            Price = 200000,
            Bedrooms = 3,
            Status = "Verkocht",
            BrokerPhone = "0612345678"
        };

        target.Merge(source);

        Assert.Equal(3, target.Bedrooms);
        Assert.Equal("Verkocht", target.Status);
        Assert.Equal("0612345678", target.BrokerPhone);

        // Price is NOT updated by Merge logic (it's handled explicitly in service)
        Assert.Equal(100000, target.Price);
    }

    [Fact]
    public void Merge_NullLabels_DoesNotCrash()
    {
        var target = new Listing { FundaId = "1", Address = "Test" };
        var source = new Listing { FundaId = "1", Address = "Test", Labels = null! };

        // Should not throw NullReferenceException
        var exception = Record.Exception(() => target.Merge(source));
        Assert.Null(exception);
    }
}
