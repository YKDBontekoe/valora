using System.ComponentModel.DataAnnotations;

namespace Valora.Api.Filters;

public class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is null) continue;

            var type = argument.GetType();
            if (type.Namespace == null || !type.Namespace.StartsWith("Valora.Application.DTOs"))
            {
                continue;
            }

            var validationContext = new ValidationContext(argument);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(argument, validationContext, validationResults, true))
            {
                var errors = validationResults
                    .GroupBy(r => r.MemberNames.FirstOrDefault() ?? string.Empty)
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(r => r.ErrorMessage ?? "Invalid value").ToArray()
                    );

                return Results.ValidationProblem(errors);
            }
        }

        return await next(context);
    }
}
