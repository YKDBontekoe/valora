using System.Collections;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Valora.Api.Filters;

public class ValidationFilter<T> : IEndpointFilter
{
    private static readonly string[] DangerousPatterns = new[]
    {
        "<script", "javascript:", "vbscript:", "onload=", "onerror=", "<img", "<svg", "<iframe", "<object", "<embed"
    };

    // Cache property info to avoid reflection overhead on every request
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();

        if (argument == null)
        {
            return Results.Problem(detail: "Invalid request parameters.", statusCode: 400);
        }

        // 1. Standard DataAnnotations Validation
        var validationContext = new ValidationContext(argument);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(argument, validationContext, validationResults, true))
        {
            var errors = validationResults
                .GroupBy(e => e.MemberNames.FirstOrDefault() ?? string.Empty)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage ?? string.Empty).ToArray()
                );

            return Results.ValidationProblem(errors);
        }

        // 2. Custom Malicious Input Detection (XSS Defense - Recursive)
        if (ContainsMaliciousInput(argument))
        {
            return Results.Problem(detail: "Potentially malicious input detected.", statusCode: 400);
        }

        return await next(context);
    }

    private bool ContainsMaliciousInput(object? obj, int depth = 0)
    {
        if (obj == null || depth > 10) return false; // Prevent infinite recursion

        var type = obj.GetType();

        // If string, check directly
        if (obj is string s)
        {
            return IsStringMalicious(s);
        }

        // If collection, iterate
        if (obj is IEnumerable collection && !(obj is string))
        {
            foreach (var item in collection)
            {
                if (ContainsMaliciousInput(item, depth + 1)) return true;
            }
            return false;
        }

        // Check if it's a safe simple type (primitive, Guid, DateTime, etc.)
        if (IsSafeType(type))
        {
            return false;
        }

        // Note: KeyValuePair is a struct (ValueType) but NOT a safe primitive, so IsSafeType returns false,
        // allowing it to proceed to property reflection below, which correctly checks Key and Value.

        // Get cached properties
        var properties = PropertyCache.GetOrAdd(type, t => t.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        foreach (var property in properties)
        {
            // Skip indexers
            if (property.GetIndexParameters().Length > 0) continue;

            // Optimization: If property type is safe, skip getting value
            if (IsSafeType(property.PropertyType)) continue;

            var value = property.GetValue(obj);
            if (value == null) continue;

            if (property.PropertyType == typeof(string))
            {
                if (IsStringMalicious((string)value)) return true;
            }
            else
            {
                // Recurse into complex object (including KeyValuePair components if not safe)
                if (ContainsMaliciousInput(value, depth + 1)) return true;
            }
        }

        return false;
    }

    private bool IsSafeType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(Guid) ||
               type == typeof(TimeSpan);
               // Removed string from here because we handle it explicitly above or want to check properties if it's somehow wrapped (unlikely but safe)
               // Actually, string IS safe to skip property reflection on (Length, Chars), but we check the string value itself in ContainsMaliciousInput.
    }

    private bool IsStringMalicious(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;

        foreach (var pattern in DangerousPatterns)
        {
            if (value.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }
}
