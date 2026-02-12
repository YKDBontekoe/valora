using System.Security.Cryptography;
using System.Text;

namespace Valora.Application.Common.Utilities;

public static class PrivacyUtils
{
    public static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "unknown";
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1) return "***" + (atIndex >= 0 ? email[atIndex..] : "");

        // Return first character + *** + domain
        return email[0] + "***" + email[atIndex..];
    }

    public static string HashEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "unknown";

        var normalizedEmail = email.Trim().ToLowerInvariant();
        var bytes = Encoding.UTF8.GetBytes(normalizedEmail);
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
