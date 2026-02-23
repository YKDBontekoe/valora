using System.Text;
using Valora.Application.Common.Utilities;
using Valora.Application.DTOs;
using Valora.Domain.Services;

namespace Valora.Application.Services.Utilities;

internal sealed class ContextReportXmlBuilder
{
    private readonly ContextReportDto _report;

    public ContextReportXmlBuilder(ContextReportDto report)
    {
        _report = report;
    }

    public string Build()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<context_report>");
        AppendHeader(sb);
        AppendCategoryScores(sb);
        AppendMetrics(sb);
        sb.AppendLine("</context_report>");
        return sb.ToString();
    }

    private void AppendHeader(StringBuilder sb)
    {
        sb.AppendLine($"  <address>{PromptSanitizer.Sanitize(_report.Location.DisplayAddress)}</address>");
        sb.AppendLine($"  <composite_score>{_report.CompositeScore:F0}</composite_score>");
    }

    private void AppendCategoryScores(StringBuilder sb)
    {
        sb.AppendLine("  <category_scores>");
        foreach (var category in _report.CategoryScores)
        {
            sb.AppendLine($"    <score category=\"{PromptSanitizer.Sanitize(category.Key)}\">{category.Value:F0}</score>");
        }
        sb.AppendLine("  </category_scores>");
    }

    private void AppendMetrics(StringBuilder sb)
    {
        sb.AppendLine("  <metrics>");
        AppendCategoryMetrics(sb, ContextScoreCalculator.CategorySocial, _report.SocialMetrics);
        AppendCategoryMetrics(sb, ContextScoreCalculator.CategorySafety, _report.CrimeMetrics);
        AppendCategoryMetrics(sb, ContextScoreCalculator.CategoryDemographics, _report.DemographicsMetrics);
        AppendCategoryMetrics(sb, ContextScoreCalculator.CategoryAmenities, _report.AmenityMetrics);
        AppendCategoryMetrics(sb, ContextScoreCalculator.CategoryEnvironment, _report.EnvironmentMetrics);
        sb.AppendLine("  </metrics>");
    }

    private void AppendCategoryMetrics(StringBuilder sb, string category, IEnumerable<ContextMetricDto> metrics)
    {
        foreach (var m in metrics)
        {
            if (m.Value.HasValue)
            {
                var scoreStr = m.Score.HasValue ? $"(Score: {m.Score:F0})" : "";
                var safeCategory = PromptSanitizer.Sanitize(category);
                var safeLabel = PromptSanitizer.Sanitize(m.Label);
                var safeUnit = PromptSanitizer.Sanitize(m.Unit);

                sb.AppendLine($"    <metric category=\"{safeCategory}\" label=\"{safeLabel}\">{m.Value} {safeUnit} {scoreStr}</metric>");
            }
        }
    }
}
