using System.ComponentModel.DataAnnotations;
using Valora.Application.DTOs;

namespace Valora.UnitTests.DTOs;

public class SecurityTests
{
    [Theory]
    [InlineData("password", false)] // Too short, no complexity
    [InlineData("Short1!", false)] // Too short
    [InlineData("Password123", false)] // No special char
    [InlineData("PASSWORD123!", false)] // No lowercase
    [InlineData("password123!", false)] // No uppercase
    [InlineData("Password!", false)] // No digit
    [InlineData("StrongPass1!", true)] // Valid
    public void RegisterDto_Password_Validation_ReturnsExpectedResult(string password, bool expectedValid)
    {
        var dto = new RegisterDto
        {
            Email = "test@example.com",
            Password = password,
            ConfirmPassword = password
        };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(null, false)] // Required
    [InlineData("", false)] // Required
    [InlineData("ab", false)] // Too short
    [InlineData("Valid Input", true)]
    // Create a string of 201 chars
    [InlineData("Lorem ipsum dolor sit amet, consectetuer adipiscing elit. Aenean commodo ligula eget dolor. Aenean massa. Cum sociis natoque penatibus et magnis dis parturient montes, nascetur ridiculus mus. Donec quExtra", false)]
    public void ContextReportRequestDto_Input_Validation_ReturnsExpectedResult(string input, bool expectedValid)
    {
        var dto = new ContextReportRequestDto(input, 1000);
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(99, false)] // Too small
    [InlineData(5001, false)] // Too large
    [InlineData(1000, true)]
    public void ContextReportRequestDto_Radius_Validation_ReturnsExpectedResult(int radius, bool expectedValid)
    {
        var dto = new ContextReportRequestDto("Valid Input", radius);
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.Equal(expectedValid, isValid);
    }

    [Theory]
    [InlineData(null, false)] // Required
    [InlineData("", false)] // Required
    [InlineData("Valid Prompt", true)]
    public void AiChatRequest_Prompt_Validation_ReturnsExpectedResult(string prompt, bool expectedValid)
    {
        var dto = new AiChatRequest { Prompt = prompt };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.Equal(expectedValid, isValid);
    }
}
