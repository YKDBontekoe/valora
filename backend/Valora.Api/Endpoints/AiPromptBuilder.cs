using System.Text;
using System.Text.RegularExpressions;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;

namespace Valora.Api.Endpoints;

public static class AiPromptBuilder
{
    private const int TokenToCharRatio = 4;

    public static AiPromptBuildResult BuildAnalysisPrompt(ContextReportDto report, AiPromptOptions options, string? model)
    {
        var limits = ResolveLimits(options, model);
        var location = RedactLocation(report.Location.DisplayAddress);
        var strongest = SelectStrongestMetrics(report, options.TopMetricCount);
        var categoryDeltas = BuildCategoryDeltas(report.CategoryScores);

        var sections = new List<string>
        {
            "You are an expert real estate analyst helping a potential resident evaluate a neighborhood.",
            $"Analyze the following location context report for: {location}",
            $"Overall Score: {report.CompositeScore:F0}/100",
            "Category deltas vs neutral baseline (50):",
            categoryDeltas,
            strongest,
            "Based on this data, provide a concise 3-4 sentence summary of the neighborhood vibe.",
            "Highlight strongest pros and most significant cons. Interpret for a human; do not list raw numbers.",
            "Use Markdown bolding for key terms and avoid guessing where data is missing."
        };

        var prompt = ApplyDeterministicBudget(sections, limits.MaxPromptChars, limits.MaxPromptTokens, options.StrictMode);
        return new AiPromptBuildResult(prompt, limits.MaxOutputTokens, BuildTelemetryTags(model, options.StrictMode, limits.TelemetryTag));
    }

    private static IReadOnlyDictionary<string, string> BuildTelemetryTags(string? model, bool strictMode, string? telemetryTag)
    {
        var tags = new Dictionary<string, string>
        {
            ["feature"] = "analyze-report",
            ["strict_mode"] = strictMode ? "true" : "false",
            ["model"] = string.IsNullOrWhiteSpace(model) ? "default" : model
        };

        if (!string.IsNullOrWhiteSpace(telemetryTag))
        {
            tags["profile"] = telemetryTag;
        }

        return tags;
    }

    private static string BuildCategoryDeltas(IReadOnlyDictionary<string, double> categoryScores)
    {
        var ordered = categoryScores
            .OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => $"- {kv.Key}: {(kv.Value - 50):+0;-0;0}");

        return string.Join(Environment.NewLine, ordered);
    }

    private static string SelectStrongestMetrics(ContextReportDto report, int topK)
    {
        var top = FlattenMetrics(report)
            .Where(x => x.Score.HasValue)
            .OrderByDescending(x => Math.Abs(x.Score!.Value - 50))
            .ThenBy(x => x.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Label, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, topK * 2))
            .ToList();

        var positives = top.Where(x => x.Score!.Value >= 50)
            .OrderByDescending(x => x.Score)
            .Take(Math.Max(1, topK))
            .Select(FormatMetricBullet)
            .ToList();

        var negatives = top.Where(x => x.Score!.Value < 50)
            .OrderBy(x => x.Score)
            .Take(Math.Max(1, topK))
            .Select(FormatMetricBullet)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"Strongest positives (top {Math.Max(1, topK)}):");
        if (positives.Count == 0) sb.AppendLine("- No high-confidence positive metrics available.");
        else positives.ForEach(x => sb.AppendLine(x));

        sb.AppendLine($"Strongest negatives (top {Math.Max(1, topK)}):");
        if (negatives.Count == 0) sb.AppendLine("- No high-confidence negative metrics available.");
        else negatives.ForEach(x => sb.AppendLine(x));

        return sb.ToString().TrimEnd();
    }

    private static string FormatMetricBullet(FlattenedMetric metric)
    {
        var value = metric.Value.HasValue ? metric.Value.Value.ToString("0.##") : "n/a";
        var unit = string.IsNullOrWhiteSpace(metric.Unit) ? string.Empty : $" {metric.Unit}";
        return $"- {metric.Category}/{metric.Label}: {value}{unit} (score {metric.Score:0})";
    }

    private static IEnumerable<FlattenedMetric> FlattenMetrics(ContextReportDto report)
    {
        return Flatten("Social", report.SocialMetrics)
            .Concat(Flatten("Safety", report.CrimeMetrics))
            .Concat(Flatten("Demographics", report.DemographicsMetrics))
            .Concat(Flatten("Housing", report.HousingMetrics))
            .Concat(Flatten("Mobility", report.MobilityMetrics))
            .Concat(Flatten("Amenities", report.AmenityMetrics))
            .Concat(Flatten("Environment", report.EnvironmentMetrics));
    }

    private static IEnumerable<FlattenedMetric> Flatten(string category, IEnumerable<ContextMetricDto> metrics)
    {
        foreach (var metric in metrics)
        {
            var label = !string.IsNullOrWhiteSpace(metric.Label)
                ? metric.Label.Trim()
                : (!string.IsNullOrWhiteSpace(metric.Key) ? metric.Key.Trim() : "unknown");

            yield return new FlattenedMetric(category, label, metric.Value, metric.Unit?.Trim(), metric.Score);
        }
    }

    private static string RedactLocation(string displayAddress)
    {
        if (string.IsNullOrWhiteSpace(displayAddress))
        {
            return "[redacted-area]";
        }

        var noPostalCode = Regex.Replace(displayAddress, @"\b\d{4}\s?[A-Z]{2}\b", string.Empty, RegexOptions.IgnoreCase);
        var noHouseNumber = Regex.Replace(noPostalCode, @"\b\d+[A-Za-z]{0,2}\b", string.Empty);

        var parts = noHouseNumber
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        if (parts.Length == 0)
        {
            return "[redacted-area]";
        }

        return parts.Length >= 2
            ? $"{parts[^2]}, {parts[^1]}"
            : parts[0];
    }

    private static string ApplyDeterministicBudget(IEnumerable<string> sections, int maxChars, int maxTokens, bool strictMode)
    {
        var targetMaxChars = Math.Max(500, Math.Min(maxChars, maxTokens * TokenToCharRatio));
        var sb = new StringBuilder();

        foreach (var section in sections)
        {
            var candidate = sb.Length == 0 ? section : $"{Environment.NewLine}{Environment.NewLine}{section}";
            if (sb.Length + candidate.Length <= targetMaxChars)
            {
                sb.Append(candidate);
                continue;
            }

            var remaining = targetMaxChars - sb.Length;
            if (remaining <= 0 || strictMode)
            {
                break;
            }

            var truncated = StableTruncate(candidate, remaining);
            if (!string.IsNullOrEmpty(truncated))
            {
                sb.Append(truncated);
            }
            break;
        }

        return sb.ToString();
    }

    private static string StableTruncate(string input, int maxChars)
    {
        if (maxChars <= 1)
        {
            return string.Empty;
        }

        if (input.Length <= maxChars)
        {
            return input;
        }

        return input[..(maxChars - 1)] + "…";
    }

    private static ResolvedLimits ResolveLimits(AiPromptOptions options, string? model)
    {
        if (!string.IsNullOrWhiteSpace(model) && options.ModelLimits.TryGetValue(model, out var modelLimit))
        {
            return new ResolvedLimits(
                modelLimit.MaxPromptChars ?? options.DefaultMaxPromptChars,
                modelLimit.MaxPromptTokens ?? options.DefaultMaxPromptTokens,
                modelLimit.MaxOutputTokens,
                modelLimit.TelemetryTag);
        }

        return new ResolvedLimits(options.DefaultMaxPromptChars, options.DefaultMaxPromptTokens, null, null);
    }

    private sealed record FlattenedMetric(string Category, string Label, double? Value, string? Unit, double? Score);
    private sealed record ResolvedLimits(int MaxPromptChars, int MaxPromptTokens, int? MaxOutputTokens, string? TelemetryTag);
}

public sealed record AiPromptBuildResult(string Prompt, int? MaxOutputTokens, IReadOnlyDictionary<string, string> TelemetryTags);
