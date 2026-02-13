using Microsoft.EntityFrameworkCore;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

namespace Valora.Infrastructure.Persistence.Extensions;

public static class ListingQueryExtensions
{
    public static IQueryable<Listing> WhereActive(this IQueryable<Listing> query)
    {
        return query.Where(l => l.Status != "Verkocht" && l.Status != "Ingetrokken" && !l.IsSoldOrRented);
    }

    public static IQueryable<Listing> ApplySearchFilter(this IQueryable<Listing> query, ListingFilterDto filter, bool isPostgres)
    {
        if (string.IsNullOrWhiteSpace(filter.SearchTerm))
            return query;

        var terms = filter.SearchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var term in terms)
        {
            if (isPostgres)
            {
                var escapedTerm = EscapeLikePattern(term);
                var search = $"%{escapedTerm}%";
                query = query.Where(l =>
                    EF.Functions.ILike(l.Address, search, @"\") ||
                    (l.City != null && EF.Functions.ILike(l.City, search, @"\")) ||
                    (l.PostalCode != null && EF.Functions.ILike(l.PostalCode, search, @"\")));
            }
            else
            {
                var search = term.ToLower();
                query = query.Where(l =>
                    l.Address.ToLower().Contains(search) ||
                    (l.City != null && l.City.ToLower().Contains(search)) ||
                    (l.PostalCode != null && l.PostalCode.ToLower().Contains(search)));
            }
        }

        return query;
    }

    public static IQueryable<Listing> ApplyCityFilter(this IQueryable<Listing> query, string? city, bool isPostgres)
    {
        if (string.IsNullOrWhiteSpace(city))
            return query;

        if (isPostgres)
        {
            var escapedCity = EscapeLikePattern(city);
            return query.Where(l => l.City != null && EF.Functions.ILike(l.City, escapedCity, @"\"));
        }
        else
        {
            return query.Where(l => l.City != null && l.City.ToLower() == city.ToLower());
        }
    }

    public static IQueryable<Listing> ApplySorting(this IQueryable<Listing> query, string? sortBy, string? sortOrder)
    {
        var sortField = sortBy?.ToLowerInvariant();
        var order = sortOrder?.ToLowerInvariant();

        return (sortField, order) switch
        {
            ("price", "desc") => query.OrderByDescending(l => l.Price),
            ("price", "asc") => query.OrderBy(l => l.Price),
            ("date", "asc") => query.OrderBy(l => l.ListedDate),
            ("date", "desc") => query.OrderByDescending(l => l.ListedDate),
            ("livingarea", "asc") => query.OrderBy(l => l.LivingAreaM2),
            ("livingarea", "desc") => query.OrderByDescending(l => l.LivingAreaM2),
            ("city", "asc") => query.OrderBy(l => l.City),
            ("city", "desc") => query.OrderByDescending(l => l.City),
            ("contextcompositescore", "asc") => query.OrderBy(l => l.ContextCompositeScore),
            ("contextcompositescore", "desc") => query.OrderByDescending(l => l.ContextCompositeScore.HasValue).ThenByDescending(l => l.ContextCompositeScore),
            ("contextsafetyscore", "asc") => query.OrderBy(l => l.ContextSafetyScore),
            ("contextsafetyscore", "desc") => query.OrderByDescending(l => l.ContextSafetyScore.HasValue).ThenByDescending(l => l.ContextSafetyScore),
            _ => query.OrderByDescending(l => l.ListedDate) // Default sort by date desc
        };
    }

    private static string EscapeLikePattern(string pattern)
    {
        return pattern
            .Replace(@"\", @"\\")
            .Replace("%", @"\%")
            .Replace("_", @"\_");
    }
}
