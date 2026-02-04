using Valora.Application.Scraping;
using Valora.Application.Validators;
using Xunit;

namespace Valora.UnitTests.Validators;

public class SearchQueryValidatorTests
{
    [Fact]
    public void IsValid_WithValidQuery_ReturnsTrue()
    {
        var query = new FundaSearchQuery { Region = "amsterdam", Page = 1, OfferingType = "buy" };
        var result = SearchQueryValidator.IsValid(query, out var error);
        Assert.True(result);
        Assert.Null(error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10001)]
    public void IsValid_WithInvalidPage_ReturnsFalse(int page)
    {
        var query = new FundaSearchQuery { Region = "amsterdam", Page = page, OfferingType = "buy" };
        var result = SearchQueryValidator.IsValid(query, out var error);
        Assert.False(result);
        Assert.Equal("Page must be between 1 and 10000", error);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    [InlineData(null)]
    public void IsValid_WithInvalidOfferingType_ReturnsFalse(string? offeringType)
    {
        // OfferingType is non-nullable in record but can be set to null via object initializer in some contexts or if binding allows
        // Here we test the validation logic specifically.
        // If the property is required/non-nullable, the compiler warns, but runtime might have null.
        // However, the record default is "buy".

        var query = new FundaSearchQuery { Region = "amsterdam", Page = 1, OfferingType = offeringType ?? "invalid" };
        var result = SearchQueryValidator.IsValid(query, out var error);
        Assert.False(result);
        Assert.Equal("Invalid OfferingType", error);
    }
}
