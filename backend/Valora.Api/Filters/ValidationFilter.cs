using System.ComponentModel.DataAnnotations;

namespace Valora.Api.Filters;

public class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is not null && ShouldValidate(argument))
            {
                var validationContext = new ValidationContext(argument);
                var validationResults = new List<ValidationResult>();

                if (!Validator.TryValidateObject(argument, validationContext, validationResults, true))
                {
                    return Results.ValidationProblem(
                        validationResults.GroupBy(
                            x => x.MemberNames.FirstOrDefault() ?? "Error",
                            x => x.ErrorMessage ?? "Invalid value"
                        ).ToDictionary(g => g.Key, g => g.ToArray())
                    );
                }
            }
        }

        return await next(context);
    }

    private static bool ShouldValidate(object argument)
    {
        var type = argument.GetType();
        // Skip primitives, strings, and system types unless they are DTOs in our namespace
        // Actually, for DTOs, we just want to validate custom classes.
        if (type.IsPrimitive || type == typeof(string) || type.IsEnum) return false;

        // Skip common framework types
        if (type.Namespace?.StartsWith("System") == true ||
            type.Namespace?.StartsWith("Microsoft") == true)
        {
            return false;
        }

        return true;
    }
}
