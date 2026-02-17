using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class AiEndpoints
{
    public static void MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai")
            .RequireAuthorization()
            .RequireRateLimiting("strict");

        group.MapPost("/chat", async (
            [FromBody] AiChatRequest request,
            IContextAnalysisService contextAnalysisService,
            ILogger<AiChatRequest> logger,
            CancellationToken ct) =>
        {
            try
            {
                var response = await contextAnalysisService.ChatAsync(request.Prompt, request.Model, ct);
                return Results.Ok(new { response });
            }
            catch (OperationCanceledException)
            {
                return Results.Problem(detail: "Request was cancelled", statusCode: 499);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing AI chat request.");
                return Results.Problem(detail: "An unexpected error occurred while processing your request.", statusCode: 500);
            }
        })
        .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<AiChatRequest>>();

        group.MapPost("/analyze-report", async (
            [FromBody] AiAnalysisRequest request,
            IContextAnalysisService contextAnalysisService,
            ILogger<AiAnalysisRequest> logger,
            CancellationToken ct) =>
        {
            try
            {
                var summary = await contextAnalysisService.AnalyzeReportAsync(request.Report, ct);
                return Results.Ok(new AiAnalysisResponse(summary));
            }
            catch (OperationCanceledException)
            {
                return Results.Problem(detail: "Request was cancelled", statusCode: 499);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error generating AI analysis report.");
                return Results.Problem(detail: "An unexpected error occurred while generating the report summary.", statusCode: 500);
            }
        })
        .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<AiAnalysisRequest>>();
    }
}
