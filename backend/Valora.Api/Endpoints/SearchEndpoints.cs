using Valora.Application.Scraping;

namespace Valora.Api.Endpoints;

public static class SearchEndpoints
{
    public static void MapSearchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization();

        group.MapGet("/search", async (
            [AsParameters] FundaSearchQuery query,
            IFundaSearchService searchService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(query.Region))
            {
                return Results.BadRequest(new { error = "Region is required" });
            }

            var result = await searchService.SearchAsync(query, ct);
            return Results.Ok(new
            {
                result.Items,
                result.TotalCount,
                result.Page,
                result.PageSize,
                result.FromCache
            });
        });

        group.MapGet("/lookup", async (
            string? url,
            IFundaSearchService searchService,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return Results.BadRequest(new { error = "URL is required" });
            }

            var listing = await searchService.GetByFundaUrlAsync(url, ct);
            return listing is null
                ? Results.NotFound(new { error = "Listing not found" })
                : Results.Ok(listing);
        });
    }
}
