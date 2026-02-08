using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public class AiChatRequest : IValidatableObject
{
    [Required]
    [MaxLength(2000)]
    public string Prompt { get; set; } = string.Empty;

    public string? Model { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!string.IsNullOrEmpty(Model))
        {
            var allowed = new[] { "gpt-4o", "claude-3-5-sonnet" };
            if (!allowed.Contains(Model))
            {
                yield return new ValidationResult($"The field Model must be one of {string.Join(", ", allowed)}.", new[] { nameof(Model) });
            }
        }
    }
}
