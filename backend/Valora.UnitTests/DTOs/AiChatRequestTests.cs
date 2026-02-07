using System.ComponentModel.DataAnnotations;
using Valora.Application.DTOs;

namespace Valora.UnitTests.DTOs;

public class AiChatRequestTests
{
    [Theory]
    [InlineData("Valid Prompt", true)]
    [InlineData("", false)] // Required implies not empty
    [InlineData(null, false)] // Required implies not null
    public void Prompt_Validation_ReturnsExpectedResult(string? prompt, bool expectedValid)
    {
        var dto = new AiChatRequest();
        if (prompt != null)
        {
            dto.Prompt = prompt;
        }
        else
        {
            // Force null assignment to test validation, suppressing compiler warning
            dto.Prompt = null!;
        }

        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.Equal(expectedValid, isValid);
    }

    [Fact]
    public void Prompt_TooLong_IsInvalid()
    {
        var dto = new AiChatRequest { Prompt = new string('a', 5001) };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Prompt"));
    }

    [Fact]
    public void Model_TooLong_IsInvalid()
    {
        var dto = new AiChatRequest { Prompt = "Valid", Model = new string('a', 101) };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.False(isValid);
        Assert.Contains(results, r => r.ErrorMessage != null && r.ErrorMessage.Contains("Model"));
    }
}
