namespace Valora.Application.DTOs;

public class UserAiProfileDto
{
    public string UserId { get; set; } = string.Empty;
    public string Preferences { get; set; } = string.Empty;
    public List<string> DisallowedSuggestions { get; set; } = new();
    public string HouseholdProfile { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public bool IsSessionOnlyMode { get; set; } = false;
    public int Version { get; set; }
}
