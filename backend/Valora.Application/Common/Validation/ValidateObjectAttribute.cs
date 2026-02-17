using System.Collections;
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

        // Handle collections of objects
        if (value is IEnumerable enumerable && !(value is string))
        {
            int index = 0;
            foreach (var item in enumerable)
            {
                if (item != null)
                {
                    var collectionItemContext = new ValidationContext(item);
                    var collectionItemResults = new List<ValidationResult>();

                    if (!Validator.TryValidateObject(item, collectionItemContext, collectionItemResults, true))
                    {
                        var compositeMessage = $"Item at index {index}: " + string.Join(" ", collectionItemResults.Select(r => r.ErrorMessage));
                        return new ValidationResult(compositeMessage);
                    }
                }
                index++;
            }
            return ValidationResult.Success;
        }

        // Handle single complex object
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
