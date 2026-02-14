using Microsoft.AspNetCore.Identity;

namespace Valora.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int DefaultRadiusMeters { get; set; } = 1000;
    public bool BiometricsEnabled { get; set; } = false;

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
