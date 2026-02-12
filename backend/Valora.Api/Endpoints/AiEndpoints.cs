using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class AiEndpoints
{
    public static void MapAiEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/ai").RequireAuthorization();

        group.MapPost("/chat", async (
            [FromBody] AiChatRequest request,
            IAiService aiService,
            ILogger<AiChatRequest> logger,
            CancellationToken ct) =>
        {
            try
            {
                var response = await aiService.ChatAsync(request.Prompt, request.Model, ct);
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
            CancellationToken ct) =>
        {
            try
            {
                var prompt = BuildAnalysisPrompt(request.Report);
                var summary = await aiService.ChatAsync(prompt, null, ct);
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

    private static string BuildAnalysisPrompt(ContextReportDto report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an expert real estate analyst helping a potential resident evaluate a neighborhood.");
        sb.AppendLine($"Analyze the following location context report for: {report.Location.DisplayAddress}");
        sb.AppendLine();

        sb.AppendLine($"**Overall Score:** {report.CompositeScore:F0}/100");
        sb.AppendLine();

        sb.AppendLine("**Category Scores:**");
        foreach (var category in report.CategoryScores)
        {
            sb.AppendLine($"- {category.Key}: {category.Value:F0}/100");
        }
        sb.AppendLine();

        sb.AppendLine("**Key Metrics:**");
        // Flatten key metrics for context
        AppendMetrics(sb, "Social", report.SocialMetrics);
        AppendMetrics(sb, "Safety", report.CrimeMetrics);
        AppendMetrics(sb, "Demographics", report.DemographicsMetrics);
        AppendMetrics(sb, "Amenities", report.AmenityMetrics);
        AppendMetrics(sb, "Environment", report.EnvironmentMetrics);

        sb.AppendLine();
        sb.AppendLine("Based on this data, provide a **concise 3-4 sentence summary** of the neighborhood vibe.");
        sb.AppendLine("Highlight the strongest pros and the most significant cons.");
        sb.AppendLine("Do not just list the numbers; interpret them for a human (e.g., 'highly walkable', 'family-friendly', 'noisy').");
        sb.AppendLine("Use Markdown bolding for key terms.");

        return sb.ToString();
    }

    private static void AppendMetrics(StringBuilder sb, string category, IEnumerable<ContextMetricDto> metrics)
    {
        foreach (var m in metrics)
        {
            if (m.Value.HasValue)
            {
                var scoreStr = m.Score.HasValue ? $"(Score: {m.Score:F0})" : "";
                sb.AppendLine($"- {category} - {m.Label}: {m.Value} {m.Unit} {scoreStr}");
            }
        }
    }
}
