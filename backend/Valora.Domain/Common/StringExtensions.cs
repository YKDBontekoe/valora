namespace Valora.Domain.Common;

public static class StringExtensions
{
    public static string? Truncate(this string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength];
    }

    public static string? Truncate(this string? value, int maxLength, out bool wasTruncated)
    {
        wasTruncated = false;
        if (string.IsNullOrEmpty(value)) return value;

        if (value.Length > maxLength)
        {
            wasTruncated = true;
            return value[..maxLength];
        }

        return value;
    }
}
