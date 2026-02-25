using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs;

public class UserAiProfileDto
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
}
