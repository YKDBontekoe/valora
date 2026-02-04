using Valora.Application.Scraping;
using Valora.Application.Validators;
using Xunit;

namespace Valora.UnitTests.Validators;

public class SearchQueryValidatorTests
{
    [Fact]
    public void IsValid_WithValidQuery_ReturnsTrue()
    {
        var query = new FundaSearchQuery(Region: "amsterdam", Page: 1, OfferingType: "buy");
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
        var query = new FundaSearchQuery(Region: "amsterdam", Page: page, OfferingType: "buy");
        var result = SearchQueryValidator.IsValid(query, out var error);
        Assert.False(result);
        Assert.Equal("Page must be between 1 and 10000", error);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("")]
    public void IsValid_WithInvalidOfferingType_ReturnsFalse(string offeringType)
    {
        var query = new FundaSearchQuery(Region: "amsterdam", Page: 1, OfferingType: offeringType);
        var result = SearchQueryValidator.IsValid(query, out var error);
        Assert.False(result);
        Assert.Equal("Invalid OfferingType", error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void IsValid_WithInvalidPageSize_ReturnsFalse(int pageSize)
    {
        var query = new FundaSearchQuery(Region: "amsterdam", PageSize: pageSize);
        var result = SearchQueryValidator.IsValid(query, out var error);
        Assert.False(result);
        Assert.Equal("PageSize must be between 1 and 100", error);
    }

    [Fact]
    public void IsValid_WithEmptyRegion_ReturnsFalse()
    {
        var query = new FundaSearchQuery(Region: "");
        var result = SearchQueryValidator.IsValid(query, out var error);
        Assert.False(result);
        Assert.Equal("Region is required", error);
    }
}
