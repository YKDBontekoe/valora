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
            .RequireRateLimiting(Valora.Api.Constants.RateLimitPolicies.Strict);

        group.MapPost("/chat", async (
            [FromBody] AiChatRequest request,
            IContextAnalysisService contextAnalysisService,
            ILogger<AiChatRequest> logger,
            CancellationToken ct) =>
        {
            try
            {
                var response = await contextAnalysisService.ChatAsync(request.Prompt, request.Feature, ct);
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

        configGroup.MapPut("/{feature}", async (
            string feature,
            [FromBody] UpdateAiModelConfigDto dto,
            IAiModelService aiModelService,
            ClaimsPrincipal user,
            ILogger<UpdateAiModelConfigDto> logger,
            CancellationToken ct) =>
        {
            if (feature != dto.Feature)
            {
                return Results.BadRequest("Feature mismatch");
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            var config = await aiModelService.GetConfigByFeatureAsync(feature, ct);
            if (config == null)
            {
                var newConfig = new AiModelConfigDto
                {
                    Feature = dto.Feature,
                    ModelId = dto.ModelId,
                    Description = dto.Description,
                    IsEnabled = dto.IsEnabled,
                    SafetySettings = dto.SafetySettings,
                    SystemPrompt = dto.SystemPrompt,
                    Temperature = dto.Temperature,
                    MaxTokens = dto.MaxTokens
                };
                var createdConfig = await aiModelService.CreateConfigAsync(newConfig, ct);

                logger.LogWarning("AUDIT: User {UserId} CREATED AI config for feature {Feature}. Model: {ModelId}",
                    userId, feature.Replace("\r", "").Replace("\n", ""), dto.ModelId.Replace("\r", "").Replace("\n", ""));

                return Results.Ok(createdConfig);
            }
            else
            {
                var oldModel = config.ModelId;

                config.ModelId = dto.ModelId;
                config.Description = dto.Description;
                config.IsEnabled = dto.IsEnabled;
                config.SafetySettings = dto.SafetySettings;
                config.SystemPrompt = dto.SystemPrompt;
                config.Temperature = dto.Temperature;
                config.MaxTokens = dto.MaxTokens;

                await aiModelService.UpdateConfigAsync(config, ct);

                logger.LogWarning("AUDIT: User {UserId} UPDATED AI config for feature {Feature}. Model: {OldModel} -> {NewModel}",
                    userId, feature.Replace("\r", "").Replace("\n", ""), oldModel.Replace("\r", "").Replace("\n", ""), dto.ModelId.Replace("\r", "").Replace("\n", ""));

                return Results.Ok(config);
            }
        })
        .AddEndpointFilter<Valora.Api.Filters.ValidationFilter<UpdateAiModelConfigDto>>();

        configGroup.MapDelete("/{id:guid}", async (
            Guid id,
            IAiModelService aiModelService,
            ClaimsPrincipal user,
            ILogger<AiModelConfigDto> logger,
            CancellationToken ct) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            await aiModelService.DeleteConfigAsync(id, ct);
            logger.LogWarning("AUDIT: User {UserId} DELETED AI config with ID {ConfigId}",
                userId, id);
            return Results.NoContent();
        });
    }
}
