using FluentAssertions;
using Valora.Application.Common.Utilities;

namespace Valora.UnitTests.Application.Common.Utilities;

public class LogSanitizerTests
{
    [Theory]
    [InlineData("Clean string", "Clean string")]
    [InlineData("String\nWith\nNewlines", "StringWithNewlines")]
    [InlineData("String\rWith\rCarriageReturns", "StringWithCarriageReturns")]
    [InlineData("String\r\nWith\r\nCRLF", "StringWithCRLF")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Sanitize_ShouldRemoveNewlinesAndCarriageReturns(string? input, string expected)
    {
        // Act
        var result = LogSanitizer.Sanitize(input);

        // Assert
        result.Should().Be(expected);
    }
}
