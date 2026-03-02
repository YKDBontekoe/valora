using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class ContextReportEndpoints
{
    /// <summary>
    /// Configures the routing and endpoints for generating Context Reports.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Architecture Decision:</strong> This endpoint serves as the entry point for Valora's core feature.
    /// It acts as an aggregator using the <strong>Fan-Out / Fan-In pattern</strong>. When a report is requested,
    /// it delegates to <see cref="IContextReportService"/> which concurrently fetches live data from multiple external sources (PDOK, CBS, OSM), rather than querying a local database.
    /// </para>
    /// </remarks>
    public static void MapContextReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/context/report")
            .RequireAuthorization()
            .RequireRateLimiting(Valora.Api.Constants.RateLimitPolicies.Strict)
            .WithTags("Context Report");

        group.MapPost("", async (
            ContextReportRequestDto request,
            IContextReportService contextReportService,
            CancellationToken ct) =>
        {
            var report = await contextReportService.BuildAsync(request, ct);
            return Results.Ok(report);
        })
        .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<ContextReportRequestDto>>();
    }
}
