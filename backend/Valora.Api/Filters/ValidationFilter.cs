using System.ComponentModel.DataAnnotations;

namespace Valora.Api.Filters;

public class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var arg in context.Arguments)
        {
            if (arg is null) continue;

            var type = arg.GetType();

            // Validate only objects in the Valora.Application.DTOs namespace
            if (type.Namespace?.StartsWith("Valora.Application.DTOs", StringComparison.Ordinal) == true)
            {
                var validationContext = new ValidationContext(arg);
                var validationResults = new List<ValidationResult>();

                if (!Validator.TryValidateObject(arg, validationContext, validationResults, true))
                {
                    var errors = validationResults
                        .GroupBy(e => e.MemberNames.FirstOrDefault() ?? "General")
                        .ToDictionary(
                            g => g.Key,
                            g => g.Select(e => e.ErrorMessage ?? "Invalid value").ToArray()
                        );

                    return Results.ValidationProblem(errors);
                }
            }
        }

        return await next(context);
    }
}
