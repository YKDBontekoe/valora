using System.Security.Cryptography;

namespace Valora.Application.Common.Utilities;

public static class PasswordGenerator
{
    /// <summary>
    /// Generates a cryptographically strong password.
    /// Requirements: 16 chars, 1 uppercase, 1 lowercase, 1 digit, 1 special char.
    /// </summary>
    public static string Generate()
    {
        const string uppers = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lowers = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string specials = "!@#$%^&*()";
        const string allChars = uppers + lowers + digits + specials;

        var chars = new char[16];

        // Ensure at least one of each required type
        chars[0] = GetRandomChar(uppers);
        chars[1] = GetRandomChar(lowers);
        chars[2] = GetRandomChar(digits);
        chars[3] = GetRandomChar(specials);

        // Fill the rest randomly
        for (int i = 4; i < chars.Length; i++)
        {
            chars[i] = GetRandomChar(allChars);
        }

        // Shuffle the result to avoid predictable pattern at start
        // Fisher-Yates shuffle
        for (int i = chars.Length - 1; i > 0; i--)
        {
            // Random index from 0 to i (inclusive)
            int j = GetRandomInt(i + 1);
            (chars[i], chars[j]) = (chars[j], chars[i]);
        }

        return new string(chars);
    }

    /// <summary>
    /// Selects a random character from the given set using unbiased rejection sampling.
    /// </summary>
    private static char GetRandomChar(string charSet)
    {
        if (string.IsNullOrEmpty(charSet)) throw new ArgumentException("Character set cannot be empty", nameof(charSet));
        return charSet[GetRandomInt(charSet.Length)];
    }

    /// <summary>
    /// Generates a random integer between 0 (inclusive) and max (exclusive).
    /// </summary>
    private static int GetRandomInt(int max)
    {
        return RandomNumberGenerator.GetInt32(max);
    }
}
