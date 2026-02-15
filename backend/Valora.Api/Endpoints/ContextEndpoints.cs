using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;

namespace Valora.Api.Endpoints;

public static class ContextEndpoints
{
    public static void MapContextEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/context").RequireRateLimiting("fixed").RequireAuthorization();

        group.MapGet("/resolve", async (string input, IContextReportService service, CancellationToken ct) =>
        {
            var result = await service.ResolveLocationAsync(input, ct);
            return result is not null ? Results.Ok(result) : Results.NotFound();
        })
        .RequireRateLimiting("strict");

        group.MapPost("/metrics/social", async ([FromBody] ResolvedLocationDto location, IContextReportService service, CancellationToken ct) =>
        {
            var warnings = new List<string>();
            var metrics = await service.GetSocialMetricsAsync(location, warnings, ct);
            var score = ContextScoreCalculator.ComputeCategoryScore(metrics);
            return Results.Ok(new { Metrics = metrics, Warnings = warnings, Score = score });
        });

        group.MapPost("/metrics/safety", async ([FromBody] ResolvedLocationDto location, IContextReportService service, CancellationToken ct) =>
        {
            var warnings = new List<string>();
            var metrics = await service.GetSafetyMetricsAsync(location, warnings, ct);
            var score = ContextScoreCalculator.ComputeCategoryScore(metrics);
            return Results.Ok(new { Metrics = metrics, Warnings = warnings, Score = score });
        });

        group.MapPost("/metrics/amenities", async ([FromBody] AmenityRequestDto request, IContextReportService service, CancellationToken ct) =>
        {
            var warnings = new List<string>();
            var metrics = await service.GetAmenityMetricsAsync(request.Location, request.RadiusMeters, warnings, ct);
            var score = ContextScoreCalculator.ComputeCategoryScore(metrics);
            return Results.Ok(new { Metrics = metrics, Warnings = warnings, Score = score });
        });

        group.MapPost("/metrics/environment", async ([FromBody] ResolvedLocationDto location, IContextReportService service, CancellationToken ct) =>
        {
            var warnings = new List<string>();
            var metrics = await service.GetEnvironmentMetricsAsync(location, warnings, ct);
            var score = ContextScoreCalculator.ComputeCategoryScore(metrics);
            return Results.Ok(new { Metrics = metrics, Warnings = warnings, Score = score });
        });

        group.MapPost("/metrics/demographics", async ([FromBody] ResolvedLocationDto location, IContextReportService service, CancellationToken ct) =>
        {
            var warnings = new List<string>();
            var metrics = await service.GetDemographicsMetricsAsync(location, warnings, ct);
            var score = ContextScoreCalculator.ComputeCategoryScore(metrics);
            return Results.Ok(new { Metrics = metrics, Warnings = warnings, Score = score });
        });

        group.MapPost("/metrics/housing", async ([FromBody] ResolvedLocationDto location, IContextReportService service, CancellationToken ct) =>
        {
            var warnings = new List<string>();
            var metrics = await service.GetHousingMetricsAsync(location, warnings, ct);
            var score = ContextScoreCalculator.ComputeCategoryScore(metrics);
            return Results.Ok(new { Metrics = metrics, Warnings = warnings, Score = score });
        });

        group.MapPost("/metrics/mobility", async ([FromBody] ResolvedLocationDto location, IContextReportService service, CancellationToken ct) =>
        {
            var warnings = new List<string>();
            var metrics = await service.GetMobilityMetricsAsync(location, warnings, ct);
            var score = ContextScoreCalculator.ComputeCategoryScore(metrics);
            return Results.Ok(new { Metrics = metrics, Warnings = warnings, Score = score });
        });
    }
}

public record AmenityRequestDto(ResolvedLocationDto Location, int RadiusMeters);
