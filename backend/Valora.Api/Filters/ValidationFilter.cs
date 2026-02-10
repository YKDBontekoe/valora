using System.ComponentModel.DataAnnotations;

namespace Valora.Api.Filters;

public class ValidationFilter<T> : IEndpointFilter where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.FirstOrDefault(a => a is T);

        if (argument == null)
        {
            return Results.BadRequest("Request body is required.");
        }

        if (argument is T validArgument)
        {
            var validationContext = new ValidationContext(validArgument);
            var validationResults = new List<ValidationResult>();

            if (!Validator.TryValidateObject(validArgument, validationContext, validationResults, true))
            {
                return Results.ValidationProblem(validationResults
                    .GroupBy(e => e.MemberNames.FirstOrDefault() ?? "General")
                    .ToDictionary(
                        g => g.Key,
                        g => g.Select(e => e.ErrorMessage ?? "Invalid value").ToArray()
                    ));
            }
        }

        return await next(context);
    }
}
