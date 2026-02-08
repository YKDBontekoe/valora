using System.ComponentModel.DataAnnotations;
using Valora.Application.DTOs;

namespace Valora.UnitTests.DTOs;

public class AiChatRequestTests
{
    [Fact]
    public void Prompt_Required_ReturnsFalse_WhenNullOrEmpty()
    {
        // Test empty string (which is now invalid due to AllowEmptyStrings = false)
        var dto = new AiChatRequest { Prompt = "" };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.False(isValid);

        // Test null
        dto.Prompt = null!;
        context = new ValidationContext(dto);
        results = new List<ValidationResult>();
        isValid = Validator.TryValidateObject(dto, context, results, true);
        Assert.False(isValid);
    }

    [Fact]
    public void Prompt_MaxLength_ReturnsFalse_WhenTooLong()
    {
        var dto = new AiChatRequest { Prompt = new string('a', 2001) };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.False(isValid);
    }

    [Theory]
    [InlineData("gpt-4o", true)]
    [InlineData("claude-3-5-sonnet", true)]
    [InlineData("invalid-model", false)]
    [InlineData(null, true)] // Nullable + AllowedValues usually means null is valid unless [Required]
    public void Model_AllowedValues_ReturnsExpectedResult(string? model, bool expectedValid)
    {
        var dto = new AiChatRequest { Prompt = "Valid Prompt", Model = model };
        var context = new ValidationContext(dto);
        var results = new List<ValidationResult>();

        var isValid = Validator.TryValidateObject(dto, context, results, true);

        Assert.Equal(expectedValid, isValid);
    }
}
