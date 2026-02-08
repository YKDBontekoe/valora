using Valora.Application.Scraping;

namespace Valora.Application.Validators;

public static class SearchQueryValidator
{
    private static readonly string[] ValidOfferingTypes = { "buy", "rent", "project" };

    public static bool IsValid(FundaSearchQuery query, out string? error)
    {
        error = null;

        if (string.IsNullOrWhiteSpace(query.Region))
        {
            error = "Region is required";
            return false;
        }

        if (query.Page < 1 || query.Page > 10000)
        {
            error = "Page must be between 1 and 10000";
            return false;
        }

        if (query.PageSize < 1 || query.PageSize > 100)
        {
            error = "PageSize must be between 1 and 100";
            return false;
        }

        if (!ValidOfferingTypes.Contains(query.OfferingType?.ToLower()))
        {
            error = "Invalid OfferingType";
            return false;
        }

        // Also run attribute validation to catch Range attributes on other properties
        var context = new System.ComponentModel.DataAnnotations.ValidationContext(query);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(query, context, results, true))
        {
            error = results.FirstOrDefault()?.ErrorMessage ?? "Validation failed";
            return false;
        }

        return true;
    }
}
