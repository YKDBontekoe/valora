using Valora.Infrastructure.Scraping;
using Valora.Infrastructure.Scraping.Models;

namespace Valora.UnitTests.Scraping;

public class FundaHtmlParserTests
{
    private readonly FundaHtmlParser _parser;

    public FundaHtmlParserTests()
    {
        _parser = new FundaHtmlParser();
    }

    [Fact]
    public void ParseSearchResults_ShouldReturnListings_WhenHtmlIsValid()
    {
        // Arrange
        var html = @"
<html>
    <body>
        <a href=""/detail/koop/amsterdam/huis-12345678-test-straat-1/"">
            Test straat 1
            <span class=""price"">€ 500.000 k.k.</span>
        </a>
        <a href=""/detail/huur/rotterdam/appartement-87654321-test-straat-2/"">
            Test straat 2
            <span class=""price"">€ 1.500 /mnd</span>
        </a>
    </body>
</html>";

        // Act
        var result = _parser.ParseSearchResults(html).ToList();

        // Assert
        Assert.Equal(2, result.Count);

        var first = result.First(x => x.FundaId == "12345678");
        Assert.Equal("https://www.funda.nl/detail/koop/amsterdam/huis-12345678-test-straat-1/", first.Url);
        Assert.Equal(500000m, first.Price);

        var second = result.First(x => x.FundaId == "87654321");
        Assert.Equal("https://www.funda.nl/detail/huur/rotterdam/appartement-87654321-test-straat-2/", second.Url);
    }

    [Fact]
    public void ParseSearchResults_ShouldHandleEmptyHtml()
    {
        // Act
        var result = _parser.ParseSearchResults("");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void ParseListingDetail_ShouldParseDetails_WhenHtmlIsValid()
    {
        // Arrange
        var html = @"
<html>
    <body>
        <h1>Test straat 1 1234 AB Amsterdam</h1>
        <span>€ 500.000 k.k.</span>
        <img src=""https://cloud.funda.nl/image.jpg"" />
        <dl>
            <dt>Wonen</dt><dd>100 m²</dd>
            <dt>Perceel</dt><dd>200 m²</dd>
            <dt>Aantal kamers</dt><dd>4 kamers (3 slaapkamers)</dd>
            <dt>Soort woonhuis</dt><dd>Eengezinswoning</dd>
            <dt>Status</dt><dd>Beschikbaar</dd>
        </dl>
    </body>
</html>";

        // Act
        var result = _parser.ParseListingDetail(html, "12345678", "http://url");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test straat 1", result!.Address);
        Assert.Equal("1234 AB", result.PostalCode);
        Assert.Equal("Amsterdam", result.City);
        Assert.Equal(500000m, result.Price);
        Assert.Equal(100, result.LivingAreaM2);
        Assert.Equal(200, result.PlotAreaM2);
        Assert.Equal(3, result.Bedrooms);
        Assert.Equal("Eengezinswoning", result.PropertyType);
        Assert.Equal("Beschikbaar", result.Status);
        Assert.Equal("https://cloud.funda.nl/image.jpg", result.ImageUrl);
    }

    [Fact]
    public void ParseListingDetail_ShouldHandleBadAddressFormat_Gracefully()
    {
        // Arrange
        var html = @"
<html>
    <body>
        <h1>Just A Street Name</h1>
        <span>€ 500.000</span>
    </body>
</html>";

        // Act
        var result = _parser.ParseListingDetail(html, "1", "url");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Just A Street Name", result!.Address);
        Assert.Null(result.PostalCode);
        Assert.Null(result.City);
    }

    [Fact]
    public void ParseListingDetail_ShouldHandlePartialAddressSplit()
    {
        // Arrange - Address with postal code but no city after it, or weird format
        var html = @"
<html>
    <body>
        <h1>Street 1234 AB</h1>
    </body>
</html>";

        // Act
        var result = _parser.ParseListingDetail(html, "1", "url");

        // Assert
        // The regex \d{4}\s*[A-Z]{2} matches "1234 AB"
        // Split by "1234 AB": ["Street ", ""]
        // parts.Length is 2. parts[0]=Street, parts[1]=""
        // So City should be empty string or null?
        // Implementation: city = parts[1].Trim() -> ""

        Assert.NotNull(result);
        Assert.Equal("Street", result!.Address);
        Assert.Equal("1234 AB", result.PostalCode);
        Assert.Equal("", result.City);
    }
}
