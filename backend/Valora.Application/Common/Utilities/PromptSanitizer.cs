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

        // 1. Regex Whitelist
        // Strip characters that are not letters, digits, standard punctuation, whitespace, symbols (\p{S}), numbers (\p{N}), or basic math symbols like < and >.
        // This whitelist allows currency symbols (€, $), units (m²), superscripts (²), and other common text while removing control characters.
        // We explicitly allow < and > so we can escape them properly in the next step.
        var sanitized = Regex.Replace(input, @"[^\w\s\p{P}\p{S}\p{N}<>]", "");

        // 2. XML Escape
        // Escape XML-like characters to prevent tag injection if we use XML-style wrapping
        // Note: Replace & first to avoid double-escaping entity references
        // Added escaping for single quotes (') as requested.
        sanitized = sanitized.Replace("&", "&amp;")
                             .Replace("\"", "&quot;")
                             .Replace("'", "&apos;")
                             .Replace("<", "&lt;")
                             .Replace(">", "&gt;");

        // 3. Truncate (Enforce maxLength on the final escaped string)
        if (sanitized.Length > maxLength)
        {
            sanitized = sanitized.Substring(0, maxLength);

            // 4. Clean up partial entities
            // If truncation cut off an XML entity (e.g. "&am"), remove the partial sequence.
            // Check for a trailing '&' or '&' followed by 1-5 characters at the end of the string.
            // XML entities we use: &amp; (5), &quot; (6), &apos; (6), &lt; (4), &gt; (4). Max length ~6 chars.
            // Regex: match an '&' followed by 0 to 5 alphanumeric chars at the end of the string.
            sanitized = Regex.Replace(sanitized, @"&[a-zA-Z0-9]{0,5}$", "");
        }

        return sanitized.Trim();
    }
}
