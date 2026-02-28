using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class ContextReportEndpoints
{
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
