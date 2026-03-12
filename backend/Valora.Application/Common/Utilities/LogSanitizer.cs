namespace Valora.Application.Common.Utilities;

public static class LogSanitizer
{
    public static string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return input;
        }

        return input.Replace(Environment.NewLine, "")
                    .Replace("\n", "")
                    .Replace("\r", "");
    }
}
