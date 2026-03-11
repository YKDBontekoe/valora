namespace Valora.Application.Common.Utilities;

public static class LogSanitizer
{
    /// <summary>
    /// Sanitizes input strings before writing them to logs.
    /// Prevents Log Injection / Log Forging by removing newline and carriage return characters.
    /// </summary>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;

        return input
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace(Environment.NewLine, " ")
            .Trim();
    }
}
