using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;

namespace Valora.Api.Endpoints;

public static class ScraperEndpoints
{
    public static void MapScraperEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/scraper").RequireAuthorization();

        group.MapPost("/trigger", (
            [FromServices] IScraperJobScheduler scheduler,
            [FromServices] IConfiguration config,
            CancellationToken ct) =>
        {
            if (!config.GetValue<bool>("HANGFIRE_ENABLED")) return Results.StatusCode(503);
            scheduler.EnqueueScraper(ct);
            return Results.Ok(new { message = "Scraper job queued" });
        });

        group.MapPost("/trigger-limited", (
            string region,
            int limit,
            [FromServices] IScraperJobScheduler scheduler,
            [FromServices] IConfiguration config,
            CancellationToken ct) =>
        {
            if (!config.GetValue<bool>("HANGFIRE_ENABLED")) return Results.StatusCode(503);
            scheduler.EnqueueLimitedScraper(region, limit, ct);
            return Results.Ok(new { message = $"Limited scraper job queued for {region} (limit {limit})" });
        });

        group.MapPost("/seed", async (
            string? region,
            [FromServices] IListingRepository repo,
            [FromServices] IScraperJobScheduler scheduler,
            [FromServices] IConfiguration config,
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                return Results.BadRequest("Region is required");
            }

            if (!config.GetValue<bool>("HANGFIRE_ENABLED")) return Results.StatusCode(503);

            var count = await repo.CountAsync(ct);
            if (count > 0)
            {
                return Results.Ok(new { message = "Data already exists, skipping seed", skipped = true });
            }

            scheduler.EnqueueSeed(region, CancellationToken.None);
            return Results.Ok(new { message = $"Seed job queued for {region}", skipped = false });
        });
    }
}
