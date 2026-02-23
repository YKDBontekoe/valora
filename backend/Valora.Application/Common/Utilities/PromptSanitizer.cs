using System.Text.RegularExpressions;

namespace Valora.Application.Common.Utilities;

public static class PromptSanitizer
{
    /// <summary>
    /// Sanitizes input strings before injecting them into the AI prompt.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Why sanitize?</strong><br/>
    /// 1. <strong>Security:</strong> Prevent "Prompt Injection" attacks where malicious user input overrides system instructions.
    ///    We treat all user input as untrusted data.
    /// 2. <strong>Token Limits:</strong> Truncate long strings to save API costs and prevent context window overflow.
    /// 3. <strong>XML/HTML Parsing:</strong> Since we wrap data in XML tags (e.g., &lt;context_report&gt;), we must escape
    ///    characters like '&lt;' and '&gt;' to prevent breaking the structure.
    /// </para>
    /// </remarks>
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
