using System;

namespace Valora.Application.Common.Utilities;

public static class LogSanitizer
{
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        return input.Replace(Environment.NewLine, "")
                    .Replace("\n", "")
                    .Replace("\r", "");
    }
}
