using Microsoft.AspNetCore.Identity;

namespace Valora.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public List<string> PreferredCities { get; set; } = new();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
