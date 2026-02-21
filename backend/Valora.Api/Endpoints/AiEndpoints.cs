using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Domain.Entities;

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
                var response = await contextAnalysisService.ChatAsync(request.Prompt, request.Intent, ct);
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

        // Config Endpoints
        var configGroup = app.MapGroup("/api/ai/config")
            .RequireAuthorization("Admin");

        configGroup.MapGet("/", async (
            IAiModelService aiModelService,
            CancellationToken ct) =>
        {
            var configs = await aiModelService.GetAllConfigsAsync(ct);
            return Results.Ok(configs);
        });

        configGroup.MapPut("/{intent}", async (
            string intent,
            [FromBody] UpdateAiModelConfigDto dto,
            IAiModelService aiModelService,
            CancellationToken ct) =>
        {
            if (intent != dto.Intent)
            {
                return Results.BadRequest("Intent mismatch");
            }

            var config = await aiModelService.GetConfigByIntentAsync(intent, ct);
            if (config == null)
            {
                var newConfig = new AiModelConfig
                {
                    Intent = dto.Intent,
                    PrimaryModel = dto.PrimaryModel,
                    FallbackModels = dto.FallbackModels,
                    Description = dto.Description,
                    IsEnabled = dto.IsEnabled,
                    SafetySettings = dto.SafetySettings
                };
                await aiModelService.CreateConfigAsync(newConfig, ct);
                return Results.Ok(newConfig);
            }
            else
            {
                config.PrimaryModel = dto.PrimaryModel;
                config.FallbackModels = dto.FallbackModels;
                config.Description = dto.Description;
                config.IsEnabled = dto.IsEnabled;
                config.SafetySettings = dto.SafetySettings;
                // SafetySettings could be updated here

                await aiModelService.UpdateConfigAsync(config, ct);
                return Results.Ok(config);
            }
        })
        .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<UpdateAiModelConfigDto>>();
    }
}
