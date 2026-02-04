using Valora.Infrastructure.Scraping;

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
}
