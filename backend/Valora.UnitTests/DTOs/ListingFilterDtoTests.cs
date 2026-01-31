using Xunit;
using Valora.Application.DTOs;

namespace Valora.UnitTests.DTOs;

public class ListingFilterDtoTests
{
    [Fact]
    public void Validate_ShouldReturnTrue_WhenInputIsValid()
    {
        var filter = new ListingFilterDto
        {
            Page = 1,
            PageSize = 20,
            SortOrder = "asc"
        };

        var isValid = filter.Validate(out var error);

        Assert.True(isValid);
        Assert.Empty(error);
    }

    [Theory]
    [InlineData(0, 10, "asc")]
    [InlineData(1, 0, "asc")]
    [InlineData(1, 101, "asc")]
    [InlineData(1, 10, "invalid")]
    public void Validate_ShouldReturnFalse_WhenInputIsInvalid(int page, int pageSize, string sortOrder)
    {
        var filter = new ListingFilterDto
        {
            Page = page,
            PageSize = pageSize,
            SortOrder = sortOrder
        };

        var isValid = filter.Validate(out var error);

        Assert.False(isValid);
        Assert.NotEmpty(error);
    }
}
