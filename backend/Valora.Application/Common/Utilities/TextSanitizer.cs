using System.Text.RegularExpressions;

namespace Valora.Application.Common.Utilities;

public static class TextSanitizer
{
    /// <summary>
    /// Sanitizes input text for safe inclusion in AI prompts.
    /// Removes non-standard characters and escapes XML-like tokens.
    /// </summary>
    /// <param name="input">The input string to sanitize.</param>
    /// <param name="maxLength">The maximum allowed length before truncation (default: 200).</param>
    /// <returns>A sanitized, safe string.</returns>
    public static string Sanitize(string? input, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Truncate first to prevent massive strings from being processed by Regex
        if (input.Length > maxLength)
        {
            input = input.Substring(0, maxLength);
        }

        // Strip characters that are not letters, digits, standard punctuation, whitespace, symbols (\p{S}), numbers (\p{N}), or basic math symbols like < and >.
        // This whitelist allows currency symbols (€, $), units (m²), superscripts (²), and other common text while removing control characters.
        // We explicitly allow < and > so we can escape them properly in the next step.
        var sanitized = Regex.Replace(input, @"[^\w\s\p{P}\p{S}\p{N}<>]", "");

        // Escape XML-like characters to prevent tag injection if we use XML-style wrapping
        // Note: Replace & first to avoid double-escaping entity references
        sanitized = sanitized.Replace("&", "&amp;")
                             .Replace("\"", "&quot;")
                             .Replace("<", "&lt;")
                             .Replace(">", "&gt;");

        return sanitized.Trim();
    }
}
