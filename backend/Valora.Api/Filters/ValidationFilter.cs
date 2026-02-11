using System.ComponentModel.DataAnnotations;

namespace Valora.Api.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.OfType<T>().FirstOrDefault();
        if (argument == null)
        {
            return Results.BadRequest("Invalid request body.");
        }

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

        return await next(context);
    }
}
