using System.ComponentModel.DataAnnotations;
using Valora.Application.DTOs;
using Xunit;

namespace Valora.UnitTests.DTOs;

public class AuthDtoTests
{
    [Fact]
    public void LoginDto_ShouldFail_WhenEmailIsTooLong()
    {
        // Arrange
        var longEmail = new string('a', 245) + "@test.com"; // 245 + 9 = 254 chars is OK, so let's make it 255 total
        longEmail = new string('a', 246) + "@test.com"; // 246 + 9 = 255 chars

        var dto = new LoginDto(longEmail, "Password123!");
        var validationContext = new ValidationContext(dto);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(LoginDto.Email)));
    }

    [Fact]
    public void RegisterDto_ShouldFail_WhenEmailIsTooLong()
    {
        // Arrange
        var longEmail = new string('a', 246) + "@test.com"; // 255 chars

        var dto = new RegisterDto
        {
            Email = longEmail,
            Password = "Password123!",
            ConfirmPassword = "Password123!"
        };
        var validationContext = new ValidationContext(dto);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(validationResults, r => r.MemberNames.Contains(nameof(RegisterDto.Email)));
    }

    [Fact]
    public void LoginDto_ShouldPass_WhenEmailIsWithinLimit()
    {
        // Arrange
        var validEmail = new string('a', 244) + "@test.com"; // 253 chars

        var dto = new LoginDto(validEmail, "Password123!");
        var validationContext = new ValidationContext(dto);
        var validationResults = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, validationContext, validationResults, true);

        // Assert
        Assert.True(isValid);
    }
}
