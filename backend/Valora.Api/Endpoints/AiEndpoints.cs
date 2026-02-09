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
            CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.Prompt))
            {
                return Results.BadRequest(new { error = "Prompt is required" });
            }

            try
            {
                var response = await aiService.ChatAsync(request.Prompt, request.Model, ct);
                return Results.Ok(new { response });
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });

        group.MapPost("/analyze-report", async (
            [FromBody] AiAnalysisRequest request,
            IAiService aiService,
            CancellationToken ct) =>
        {
            if (request.Report is null)
            {
                return Results.BadRequest(new { error = "Report data is required" });
            }

            try
            {
                var prompt = BuildAnalysisPrompt(request.Report);
                var summary = await aiService.ChatAsync(prompt, null, ct);
                return Results.Ok(new AiAnalysisResponse(summary));
            }
            catch (Exception ex)
            {
                return Results.Problem(detail: ex.Message, statusCode: 500);
            }
        });
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
