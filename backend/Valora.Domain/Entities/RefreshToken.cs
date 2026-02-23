using System.Security.Cryptography;
using System.Text;
using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string TokenHash { get; set; } = string.Empty;
    public string RawToken { get; set; } = string.Empty;
    public DateTime Expires { get; set; }
    public DateTime? Revoked { get; set; }
    public bool IsExpired => DateTime.UtcNow >= Expires;
    public bool IsActive => Revoked == null && !IsExpired;

    public string UserId { get; set; } = string.Empty;
    public ApplicationUser? User { get; set; }

    public static RefreshToken Create(string userId, TimeProvider timeProvider)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty.", nameof(userId));
        }

        ArgumentNullException.ThrowIfNull(timeProvider);

        var randomNumber = new byte[64];
        RandomNumberGenerator.Fill(randomNumber);

        var rawToken = Convert.ToBase64String(randomNumber);
        var now = timeProvider.GetUtcNow().UtcDateTime;

        return new RefreshToken
        {
            RawToken = rawToken,
            TokenHash = ComputeHash(rawToken),
            Expires = now.AddDays(30),
            CreatedAt = now,
            UserId = userId
        };
    }

    public static string ComputeHash(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new ArgumentException("Token cannot be null or empty.", nameof(rawToken));
        }

        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }
}
