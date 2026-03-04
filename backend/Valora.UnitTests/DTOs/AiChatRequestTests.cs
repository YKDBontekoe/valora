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
    [InlineData("custom_feature_123", true)] // Added flexible feature check
    [InlineData("invalid feature", false)] // Spaces not allowed
    [InlineData("", false)]
    [InlineData(null, false)]
    public void Feature_Validation(string? feature, bool isValid)
    {
        var request = new AiChatRequest
        {
            Prompt = "Test Prompt"
        };

        // We need to set Feature explicitly, as the property initializer sets a default.
        // If the test case provides null, we want to test validation of null.
        if (feature != null)
        {
            request.Feature = feature;
        }
        else
        {
            // Reflection hack or just accept that the DTO might initialize it.
            // Ideally, we test the property setter.
            // For ValidationContext to catch null on a non-nullable property, it usually needs [Required].
            // However, our DTO initializes it to "chat".
            // To test null validation, we force it to null.
            typeof(AiChatRequest).GetProperty("Feature")!.SetValue(request, null);
        }

        var context = new ValidationContext(request);
        var results = new List<ValidationResult>();
        var result = Validator.TryValidateObject(request, context, results, true);

        Assert.Equal(isValid, result);
        if (!isValid)
        {
            Assert.Contains(results, r => r.MemberNames.Contains("Feature"));
        }
    }
}
