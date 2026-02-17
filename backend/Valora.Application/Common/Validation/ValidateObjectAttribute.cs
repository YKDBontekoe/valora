using System.ComponentModel.DataAnnotations;

namespace Valora.Application.Common.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class ValidateObjectAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is null)
        {
            return ValidationResult.Success;
        }

        var context = new ValidationContext(value);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(value, context, results, true))
        {
            // Aggregate errors
            var compositeMessage = string.Join(" ", results.Select(r => r.ErrorMessage));
            return new ValidationResult(compositeMessage);
        }

        return ValidationResult.Success;
    }
}
