using System.ComponentModel.DataAnnotations;
using Valora.Application.DTOs;
using Xunit;

namespace Valora.UnitTests.DTOs;

public class AiChatRequestTests
{
    [Theory]
    [InlineData("quick_summary", true)]
    [InlineData("detailed_analysis", true)]
    [InlineData("chat", true)]
    [InlineData("custom_intent_123", true)] // Added flexible intent check
    [InlineData("invalid intent", false)] // Spaces not allowed
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Intent_Validation(string intent, bool isValid)
    {
        var request = new AiChatRequest
        {
            Prompt = "Test Prompt",
            Intent = intent
        };

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        var result = Validator.TryValidateObject(request, context, results, true);

        Assert.Equal(isValid, result);
        if (!isValid)
        {
            Assert.Contains(results, r => r.MemberNames.Contains("Intent"));
        }
    }
}
