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
        if (DisallowedSuggestions == null) yield break;

        foreach (var suggestion in DisallowedSuggestions)
        {
            if (suggestion == null)
            {
                 yield return new ValidationResult(
                    "Disallowed suggestions cannot contain null values.",
                    new[] { nameof(DisallowedSuggestions) });
                 continue;
            }

            if (suggestion.Length > 100)
            {
                yield return new ValidationResult(
                    "Each disallowed suggestion must be 100 characters or less.",
                    new[] { nameof(DisallowedSuggestions) });
            }
        }

        var json = System.Text.Json.JsonSerializer.Serialize(DisallowedSuggestions);
        if (json.Length > 4000)
        {
             yield return new ValidationResult(
                    "Total size of disallowed suggestions exceeds the limit.",
                    new[] { nameof(DisallowedSuggestions) });
        }
    }
}
