using Microsoft.Extensions.Options;
using Valora.Application.Common.Models;
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
            IAiService aiService,
            ILogger<AiChatRequest> logger,
            CancellationToken ct) =>
        {
            try
            {
                var response = await aiService.ChatAsync(request.Prompt, request.Model, null, ct);
                return Results.Ok(new { response });
            }
            catch (OperationCanceledException)
            {
                // Client cancelled the request - don't log as error
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
            IAiService aiService,
            ILogger<AiAnalysisRequest> logger,
            IOptions<AiPromptOptions> promptOptions,
            IConfiguration configuration,
            CancellationToken ct) =>
        {
            try
            {
                var model = configuration["OPENROUTER_MODEL"];
                var promptResult = AiPromptBuilder.BuildAnalysisPrompt(request.Report, promptOptions.Value, model);
                var summary = await aiService.ChatAsync(promptResult.Prompt, model, new AiExecutionOptions(promptResult.MaxOutputTokens, promptResult.TelemetryTags), ct);
                return Results.Ok(new AiAnalysisResponse(summary));
            }
            catch (OperationCanceledException)
            {
                // Client cancelled the request - don't log as error
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
