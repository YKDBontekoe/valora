using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public class UserAiProfileDto : IValidatableObject
{
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [StringLength(4000)]
    public string Preferences { get; set; } = string.Empty;

    [MaxLength(20)]
    public List<string> DisallowedSuggestions { get; set; } = new();

    [StringLength(4000)]
    public string HouseholdProfile { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsSessionOnlyMode { get; set; } = false;
    public int Version { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // JSON serialization overhead estimate (e.g. quotes, commas, brackets)
        // Simple check: Sum of string lengths + overhead must be < 4000
        // Or simpler: each string max 100 chars, 20 items = 2000 chars + overhead << 4000

        foreach (var suggestion in DisallowedSuggestions)
        {
            if (suggestion.Length > 100)
            {
                yield return new ValidationResult(
                    "Each disallowed suggestion must be 100 characters or less.",
                    new[] { nameof(DisallowedSuggestions) });
            }
        }

        // Approximate JSON size check
        var estimatedSize = 2 + (DisallowedSuggestions.Count > 0 ? DisallowedSuggestions.Sum(s => s.Length + 4) - 1 : 0);
        if (estimatedSize > 4000)
        {
             yield return new ValidationResult(
                    "Total size of disallowed suggestions exceeds the limit.",
                    new[] { nameof(DisallowedSuggestions) });
        }
    }
}
