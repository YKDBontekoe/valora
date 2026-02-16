using FluentAssertions;
using Valora.Domain.Common;

namespace Valora.UnitTests.Domain.Common;

public class StringExtensionsTests
{
    [Fact]
    public void Truncate_ShouldReturnNull_WhenInputIsNull()
    {
        string? input = null;
        var result = input.Truncate(10);
        result.Should().BeNull();
    }

    [Fact]
    public void Truncate_ShouldReturnEmptyString_WhenInputIsEmpty()
    {
        var result = string.Empty.Truncate(10);
        result.Should().BeEmpty();
    }

    [Fact]
    public void Truncate_ShouldReturnOriginalString_WhenLengthIsLessOrEqual()
    {
        var input = "Short";
        var result = input.Truncate(10);
        result.Should().Be(input);
    }

    [Fact]
    public void Truncate_ShouldReturnTruncatedString_WhenLengthExceedsLimit()
    {
        var input = "LongStringForTest";
        var result = input.Truncate(4);
        result.Should().Be("Long");
    }

    [Fact]
    public void TruncateWithOut_ShouldReturnFalse_WhenNotTruncated()
    {
        var input = "Short";
        var result = input.Truncate(10, out bool wasTruncated);

        result.Should().Be(input);
        wasTruncated.Should().BeFalse();
    }

    [Fact]
    public void TruncateWithOut_ShouldReturnTrue_WhenTruncated()
    {
        var input = "LongStringForTest";
        var result = input.Truncate(4, out bool wasTruncated);

        result.Should().Be("Long");
        wasTruncated.Should().BeTrue();
    }

    [Fact]
    public void TruncateWithOut_ShouldHandleNullCorrectly()
    {
        string? input = null;
        var result = input.Truncate(10, out bool wasTruncated);

        result.Should().BeNull();
        wasTruncated.Should().BeFalse();
    }
}
