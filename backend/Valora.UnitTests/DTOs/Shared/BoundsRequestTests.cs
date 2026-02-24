using System.ComponentModel.DataAnnotations;
using Valora.Application.DTOs.Shared;
using Xunit;

namespace Valora.UnitTests.DTOs.Shared;

public class BoundsRequestTests
{
    [Fact]
    public void Validate_ShouldPass_WhenCrossingDateLine()
    {
        // Arrange
        var request = new BoundsRequest(
            MinLat: 10,
            MinLon: 179,
            MaxLat: 20,
            MaxLon: -179
        );

        // Act
        var validationContext = new ValidationContext(request);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(request, validationContext, validationResults, true);

        // Assert
        Assert.True(isValid);
        Assert.Empty(validationResults);
    }
}
