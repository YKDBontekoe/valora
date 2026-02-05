using Microsoft.Extensions.Logging;
using Moq;
using Valora.Infrastructure.Scraping;

namespace Valora.UnitTests.Scraping;

public class FundaNuxtJsonParserTests
{
    [Fact]
    public void Parse_ValidJson_ReturnsData()
    {
        var json = @"{
            ""someKey"": {
                ""features"": {},
                ""media"": {},
                ""description"": { ""content"": ""test"" }
            }
        }";

        var result = FundaNuxtJsonParser.Parse(json);

        Assert.NotNull(result);
        Assert.Equal("test", result.Description?.Content);
    }

    [Fact]
    public void Parse_InvalidJson_LogsErrorAndReturnsNull()
    {
        var loggerMock = new Mock<ILogger>();
        var json = "{ invalid json }";

        var result = FundaNuxtJsonParser.Parse(json, loggerMock.Object);

        Assert.Null(result);

        // Verify logger was called with Error level
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public void Parse_JsonMissingRequiredFields_ReturnsNull()
    {
        var json = @"{
            ""someKey"": {
                ""features"": {},
                ""media"": {}
                // missing description
            }
        }";

        var result = FundaNuxtJsonParser.Parse(json);

        Assert.Null(result);
    }

    [Fact]
    public void Parse_DeeplyNested_FoundData()
    {
         var json = @"{
            ""level1"": {
                ""level2"": {
                    ""target"": {
                        ""features"": {},
                        ""media"": {},
                        ""description"": { ""content"": ""deep"" }
                    }
                }
            }
        }";

        var result = FundaNuxtJsonParser.Parse(json);

        Assert.NotNull(result);
        Assert.Equal("deep", result.Description?.Content);
    }
}
