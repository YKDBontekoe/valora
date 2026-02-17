using System.Collections;
using System.ComponentModel.DataAnnotations;

namespace Valora.Application.Common.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class MaxCollectionSizeAttribute : ValidationAttribute
{
    private readonly int _maxSize;

    public MaxCollectionSizeAttribute(int maxSize)
    {
        _maxSize = maxSize;
        ErrorMessage = "The collection {0} cannot contain more than {1} items.";
    }

    public override bool IsValid(object? value)
    {
        if (value is null)
        {
            return true;
        }

        if (value is ICollection collection)
        {
            return collection.Count <= _maxSize;
        }

        if (value is IEnumerable enumerable)
        {
            int count = 0;
            foreach (var _ in enumerable)
            {
                count++;
                if (count > _maxSize) return false;
            }
            return true;
        }

        return false;
    }

    public override string FormatErrorMessage(string name)
    {
        return string.Format(System.Globalization.CultureInfo.CurrentCulture, ErrorMessageString, name, _maxSize);
    }
}
