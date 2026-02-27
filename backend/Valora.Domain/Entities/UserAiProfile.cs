namespace Valora.Domain.Entities;

public class UserAiProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string UserId { get; set; } = string.Empty;

    public string Preferences { get; set; } = string.Empty;

    public List<string> DisallowedSuggestions { get; set; } = new();

    public string HouseholdProfile { get; set; } = string.Empty;

    public bool IsEnabled { get; set; } = true;

    public bool IsSessionOnlyMode { get; set; } = false;

    public int Version { get; set; } = 1;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}
