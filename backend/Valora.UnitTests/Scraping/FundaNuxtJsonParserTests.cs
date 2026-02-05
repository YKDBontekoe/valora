using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
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

    [Fact]
    public void Parse_ExceedsSafetyCounter_ReturnsNull()
    {
        // Construct a deeply nested JSON object that exceeds the 10000 iteration limit
        // Since the parser uses BFS, a wide structure is more effective than deep for hitting the limit quickly
        // if we just want to fill the queue. But BFS visits layer by layer.
        // The safety limit is 10000.
        // Let's create a JSON with an array of 10001 empty objects.

        var sb = new StringBuilder();
        sb.Append("{\"items\": [");
        for (int i = 0; i < 10005; i++)
        {
            sb.Append("{},");
        }
        sb.Append("{} ]}"); // Close array

        var json = sb.ToString();

        var result = FundaNuxtJsonParser.Parse(json);

        Assert.Null(result);
    }
}
