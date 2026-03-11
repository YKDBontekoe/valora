using System.Text.RegularExpressions;

namespace Valora.Application.Common.Utilities;

public static class LogSanitizer
{
    /// <summary>
    /// Sanitizes input strings before writing them to logs.
    /// Prevents Log Injection / Log Forging by normalizing newline and carriage return characters to a single space,
    /// and collapsing any resulting consecutive whitespace.
    /// </summary>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        // Replace all newline variants (\r\n, \r, \n) with a space
        var noNewlines = Regex.Replace(input, @"\r\n|\r|\n", " ");

        // Collapse multiple whitespace characters (including tabs) into a single space
        return Regex.Replace(noNewlines, @"\s+", " ").Trim();
    }
}
