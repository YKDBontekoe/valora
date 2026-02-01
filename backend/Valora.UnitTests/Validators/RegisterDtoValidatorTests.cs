using FluentValidation.TestHelper;
using Valora.Application.DTOs;
using Valora.Application.Validators;

namespace Valora.UnitTests.Validators;

public class RegisterDtoValidatorTests
{
    private readonly RegisterDtoValidator _validator = new();

    [Fact]
    public void Should_Have_Error_When_Email_Is_Empty()
    {
        var model = new RegisterDto { Email = "", Password = "Password1!", ConfirmPassword = "Password1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Have_Error_When_Email_Is_Invalid()
    {
        var model = new RegisterDto { Email = "invalid-email", Password = "Password1!", ConfirmPassword = "Password1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Email_Is_Valid()
    {
        var model = new RegisterDto { Email = "test@example.com", Password = "Password1!", ConfirmPassword = "Password1!" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("short")] // Too short
    [InlineData("nouppercase1!")] // No uppercase
    [InlineData("NOLOWERCASE1!")] // No lowercase
    [InlineData("NoNumber!")] // No number
    [InlineData("NoSpecialChar1")] // No special char
    public void Should_Have_Error_When_Password_Does_Not_Meet_Complexity(string password)
    {
        var model = new RegisterDto { Email = "test@example.com", Password = password, ConfirmPassword = password };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Password_Is_Valid()
    {
        var model = new RegisterDto { Email = "test@example.com", Password = "ValidPassword1!", ConfirmPassword = "ValidPassword1!" };
        var result = _validator.TestValidate(model);
        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Should_Have_Error_When_Passwords_Do_Not_Match()
    {
        var model = new RegisterDto { Email = "test@example.com", Password = "Password1!", ConfirmPassword = "DifferentPassword1!" };
        var result = _validator.TestValidate(model);
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword);
    }
}
