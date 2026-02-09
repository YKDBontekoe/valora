using System.ComponentModel.DataAnnotations;

namespace Valora.Api.Filters;

public class ValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        foreach (var argument in context.Arguments)
        {
            if (argument is not null)
            {
                var validationContext = new ValidationContext(argument);
                var validationResults = new List<ValidationResult>();

                if (!Validator.TryValidateObject(argument, validationContext, validationResults, true))
                {
                    return Results.BadRequest(validationResults.Select(r => new
                    {
                        Property = r.MemberNames.FirstOrDefault(),
                        Error = r.ErrorMessage
                    }));
                }
            }
        }

        return await next(context);
    }
}
