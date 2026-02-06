using System.ComponentModel.DataAnnotations;
using Valora.Application.DTOs;
using Xunit;

namespace Valora.UnitTests.DTOs;

public class RegisterDtoTests
{
    private IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void RegisterDto_WithValidPassword_ShouldPassValidation()
    {
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = "StrongPassword123!",
            ConfirmPassword = "StrongPassword123!"
        };

        var results = ValidateModel(dto);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("short1!", "Password must be at least 8 characters long")]
    [InlineData("nocapitals123!", "Password must contain at least one uppercase letter")]
    [InlineData("NO_LOWERCASE_123!", "Password must contain at least one lowercase letter")]
    [InlineData("NoDigits!", "Password must contain at least one digit")]
    [InlineData("NoSpecialChars123", "Password must contain at least one non-alphanumeric character")]
    public void RegisterDto_WithWeakPassword_ShouldFailValidation(string password, string reason)
    {
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = password,
            ConfirmPassword = password
        };

        var results = ValidateModel(dto);

        Assert.NotEmpty(results);

        // Ensure the error is related to Password
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(RegisterDto.Password)));

        // Check if the validation failed. We don't strictly check the message content
        // because the Regex one is a single catch-all message.
        // reason parameter is kept for test case documentation.
        Assert.True(!string.IsNullOrEmpty(reason));
    }
}
