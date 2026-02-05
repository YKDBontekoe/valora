using System.ComponentModel.DataAnnotations;
using Valora.Application.DTOs;

namespace Valora.UnitTests.Security;

public class AuthSecurityTests
{
    [Fact]
    public void RegisterDto_PasswordShort_FailsValidation()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "short",
            ConfirmPassword = "short"
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.False(isValid);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterDto.Password)));
        Assert.Contains(results, r => r.ErrorMessage!.Contains("at least 8 characters"));
    }

    [Fact]
    public void RegisterDto_PasswordLongEnough_PassesValidation()
    {
        // Arrange
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "StrongPassword123!",
            ConfirmPassword = "StrongPassword123!"
        };

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        // Act
        var isValid = Validator.TryValidateObject(dto, context, results, true);

        // Assert
        Assert.True(isValid);
    }
}
