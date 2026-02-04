using Valora.Application.Scraping;

namespace Valora.Application.Validators;

public static class SearchQueryValidator
{
    public static bool IsValid(FundaSearchQuery query, out string? error)
    {
        error = null;

        if (query.Page < 1 || query.Page > 10000)
        {
            error = "Page must be between 1 and 10000";
            return false;
        }

        if (!new[] { "buy", "rent", "project" }.Contains(query.OfferingType?.ToLower()))
        {
            error = "Invalid OfferingType";
            return false;
        }

        return true;
    }
}
