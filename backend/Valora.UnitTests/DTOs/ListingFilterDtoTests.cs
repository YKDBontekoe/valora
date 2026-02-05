using System.ComponentModel.DataAnnotations;
using Valora.Application.DTOs;

namespace Valora.UnitTests.DTOs;

public class ListingFilterDtoTests
{
    [Theory]
    [InlineData("Price", true)]
    [InlineData("Date", true)]
    [InlineData("LivingArea", true)]
    [InlineData("City", true)]
    [InlineData("price", true)] // Case insensitive regex (?i)
    [InlineData("city", true)]
    [InlineData("Invalid", false)]
    [InlineData("", true)] // RegularExpressionAttribute allows empty strings by default.
    public void SortBy_Validation_ReturnsExpectedResult(string sortBy, bool expectedValid)
    {
        var dto = new ListingFilterDto { SortBy = sortBy };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public void SortBy_Null_IsValid()
    {
        var dto = new ListingFilterDto { SortBy = null };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.True(isValid);
    }
}
