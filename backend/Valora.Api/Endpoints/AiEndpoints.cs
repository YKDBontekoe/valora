using System.Security.Claims;
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

        configGroup.MapGet("/models", async (
            IAiService aiService,
            CancellationToken ct) =>
        {
            try
            {
                var models = await aiService.GetAvailableModelsAsync(ct);
                return Results.Ok(models);
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });

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
            ClaimsPrincipal user,
            ILogger<UpdateAiModelConfigDto> logger,
            CancellationToken ct) =>
        {
            if (intent != dto.Intent)
            {
                return Results.BadRequest("Intent mismatch");
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = user.FindFirstValue(ClaimTypes.Email);

            var config = await aiModelService.GetConfigByIntentAsync(intent, ct);
            if (config == null)
            {
                var newConfig = new AiModelConfigDto
                {
                    Intent = dto.Intent,
                    PrimaryModel = dto.PrimaryModel,
                    FallbackModels = dto.FallbackModels,
                    Description = dto.Description,
                    IsEnabled = dto.IsEnabled,
                    SafetySettings = dto.SafetySettings
                };
                var createdConfig = await aiModelService.CreateConfigAsync(newConfig, ct);

                logger.LogWarning("AUDIT: User {UserEmail} ({UserId}) CREATED AI config for intent {Intent}. Primary: {PrimaryModel}",
                    userEmail, userId, intent, dto.PrimaryModel);

                return Results.Ok(createdConfig);
            }
            else
            {
                var oldPrimary = config.PrimaryModel;

                config.PrimaryModel = dto.PrimaryModel;
                config.FallbackModels = dto.FallbackModels;
                config.Description = dto.Description;
                config.IsEnabled = dto.IsEnabled;
                config.SafetySettings = dto.SafetySettings;

                await aiModelService.UpdateConfigAsync(config, ct);

                logger.LogWarning("AUDIT: User {UserEmail} ({UserId}) UPDATED AI config for intent {Intent}. Primary: {OldModel} -> {NewModel}",
                    userEmail, userId, intent, oldPrimary, dto.PrimaryModel);

                return Results.Ok(config);
            }
        })
        .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<UpdateAiModelConfigDto>>();
    }
}
