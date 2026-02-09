using System.ComponentModel.DataAnnotations;
using Valora.Application.DTOs;

namespace Valora.UnitTests.DTOs;

public class DtoValidationTests
{
    [Fact]
    public void LoginDto_ValidatesCorrectly()
    {
        AssertValid(new LoginDto("test@example.com", "password"));
        AssertInvalid(new LoginDto("invalid-email", "password"), nameof(LoginDto.Email));
        AssertInvalid(new LoginDto(null!, "password"), nameof(LoginDto.Email));
        AssertInvalid(new LoginDto("test@example.com", null!), nameof(LoginDto.Password));
    }

    [Fact]
    public void RegisterDto_ValidatesCorrectly()
    {
        AssertValid(new RegisterDto { Email = "test@example.com", Password = "password", ConfirmPassword = "password" });
        AssertInvalid(new RegisterDto { Email = "invalid", Password = "p", ConfirmPassword = "p" }, nameof(RegisterDto.Email));
    }

    [Fact]
    public void ContextReportRequestDto_ValidatesCorrectly()
    {
        AssertValid(new ContextReportRequestDto("Some Input", 1000));
        AssertInvalid(new ContextReportRequestDto(null!, 1000), nameof(ContextReportRequestDto.Input));
        AssertInvalid(new ContextReportRequestDto("", 1000), nameof(ContextReportRequestDto.Input));
        AssertInvalid(new ContextReportRequestDto("Input", 50), nameof(ContextReportRequestDto.RadiusMeters));
        AssertInvalid(new ContextReportRequestDto("Input", 10000), nameof(ContextReportRequestDto.RadiusMeters));
    }

    [Fact]
    public void AiChatRequest_ValidatesCorrectly()
    {
        AssertValid(new AiChatRequest { Prompt = "Hello" });
        AssertInvalid(new AiChatRequest { Prompt = "" }, nameof(AiChatRequest.Prompt));
        AssertInvalid(new AiChatRequest { Prompt = new string('a', 5001) }, nameof(AiChatRequest.Prompt));
    }

    private void AssertValid(object model)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, context, results, true);
        Assert.True(isValid, $"Expected valid, but got errors: {string.Join(", ", results.Select(r => r.ErrorMessage))}");
    }

    private void AssertInvalid(object model, string expectedMember)
    {
        var context = new ValidationContext(model);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(model, context, results, true);
        Assert.False(isValid, "Expected invalid model");
        Assert.Contains(results, r => r.MemberNames.Contains(expectedMember));
    }
}
