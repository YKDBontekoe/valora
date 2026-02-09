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

        // Only validate types in the DTOs namespace
        // This avoids validating injected services, generic types, or framework objects
        return type.Namespace?.StartsWith("Valora.Application.DTOs") == true;
    }
}
