using Valora.Application.Scraping.Utils;

namespace Valora.UnitTests.Scraping;

public class FundaUrlParserTests
{
    [Theory]
    [InlineData("https://www.funda.nl/koop/amsterdam/", "amsterdam")]
    [InlineData("https://www.funda.nl/huur/rotterdam/", "rotterdam")]
    [InlineData("https://www.funda.nl/koop/den-haag/", "den-haag")]
    [InlineData("https://www.funda.nl/zoeken/koop?selected_area=%5B%22nl%2Fgroningen%22%5D", "groningen")]
    [InlineData("https://www.funda.nl/zoeken/koop?selected_area=%22amstelveen%22", "amstelveen")]
    public void ExtractRegionFromUrl_ValidUrls_ReturnsRegion(string url, string expectedRegion)
    {
        var result = FundaUrlParser.ExtractRegionFromUrl(url);

        Assert.NotNull(result);
        Assert.Contains(expectedRegion, result, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("https://www.funda.nl/invalid")]
    [InlineData("")]
    public void ExtractRegionFromUrl_InvalidUrls_ReturnsNull(string url)
    {
        var result = FundaUrlParser.ExtractRegionFromUrl(url);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("https://www.funda.nl/detail/koop/amsterdam/appartement-42424242-test/42424242/", 42424242)]
    [InlineData("https://www.funda.nl/detail/huur/rotterdam/huis-123456/123456/", 123456)]
    [InlineData("https://www.funda.nl/43224373/", 43224373)]
    public void ExtractGlobalIdFromUrl_ValidUrls_ReturnsId(string url, int expectedId)
    {
        var result = FundaUrlParser.ExtractGlobalIdFromUrl(url);
        Assert.Equal(expectedId, result);
    }
}
