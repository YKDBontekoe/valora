using System.ComponentModel.DataAnnotations;
using Valora.Application.DTOs;
using Xunit;

namespace Valora.UnitTests.DTOs;

public class AiChatRequestTests
{
    private IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var ctx = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, ctx, validationResults, true);
        return validationResults;
    }

    [Fact]
    public void AiChatRequest_WithValidData_ShouldPassValidation()
    {
        var request = new AiChatRequest
        {
            Prompt = "Hello, world!",
            Model = "gpt-4"
        };

        var results = ValidateModel(request);

        Assert.Empty(results);
    }

    [Fact]
    public void AiChatRequest_WithEmptyPrompt_ShouldFailValidation()
    {
        var request = new AiChatRequest
        {
            Prompt = "",
            Model = "gpt-4"
        };

        var results = ValidateModel(request);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains("Prompt"));
    }

    [Fact]
    public void AiChatRequest_WithOversizedPrompt_ShouldFailValidation()
    {
        var request = new AiChatRequest
        {
            Prompt = new string('a', 5001), // Assuming max 5000
            Model = "gpt-4"
        };

        var results = ValidateModel(request);

        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains("Prompt"));
    }
}
