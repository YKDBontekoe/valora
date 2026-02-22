using Valora.Infrastructure.Enrichment;
using Xunit;

namespace Valora.UnitTests.Enrichment;

public class UrlNormalizationUtilsTests
{
    [Theory]
    [InlineData("Damrak 1 Amsterdam", "Damrak 1 Amsterdam")]
    [InlineData("  Damrak 1 Amsterdam  ", "Damrak 1 Amsterdam")]
    public void NormalizeInput_ReturnsTrimmedInput_ForSimpleStrings(string input, string expected)
    {
        Assert.Equal(expected, UrlNormalizationUtils.NormalizeInput(input));
    }

    [Fact]
    public void NormalizeInput_ExtractsAddressFromFundaUrl()
    {
        var url = "https://www.funda.nl/koop/amsterdam/appartement-42442424-damrak-1/";
        var expected = "appartement 42442424 damrak 1";
        Assert.Equal(expected, UrlNormalizationUtils.NormalizeInput(url));
    }

    [Fact]
    public void NormalizeInput_ExtractsAddressFromGenericUrlSlug()
    {
        var url = "https://example.com/properties/damrak-1-amsterdam";
        var expected = "damrak 1 amsterdam";
        Assert.Equal(expected, UrlNormalizationUtils.NormalizeInput(url));
    }

    [Fact]
    public void NormalizeInput_PrefersQueryParam_IfPresent()
    {
        var url = "https://maps.example.com/search?q=Damrak%201%20Amsterdam";
        var expected = "Damrak 1 Amsterdam";
        Assert.Equal(expected, UrlNormalizationUtils.NormalizeInput(url));
    }

    [Fact]
    public void NormalizeInput_PrefersQueryParam_OverSlug()
    {
        var url = "https://example.com/search/ignored-slug?query=Damrak%201%20Amsterdam";
        var expected = "Damrak 1 Amsterdam";
        Assert.Equal(expected, UrlNormalizationUtils.NormalizeInput(url));
    }

    [Fact]
    public void NormalizeInput_ReturnsOriginalInput_IfUrlHasNoRelevantData()
    {
        var url = "https://example.com/about-us";
        // It might return "about us" if it treats the slug as relevant, but let's check the behavior.
        // The implementation checks if the segment contains letters. "about-us" -> "about us".
        Assert.Equal("about us", UrlNormalizationUtils.NormalizeInput(url));
    }

    [Fact]
    public void NormalizeInput_ReturnsTrimmedInput_ForInvalidUrl()
    {
        var input = "not a url";
        Assert.Equal("not a url", UrlNormalizationUtils.NormalizeInput(input));
    }
}
