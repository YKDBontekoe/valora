using Valora.Api.Endpoints;
using Valora.Application.Common.Models;
using Valora.Application.DTOs;

namespace Valora.UnitTests.Services;

public class AiPromptBuilderTests
{
    [Fact]
    public void BuildAnalysisPrompt_ShouldTruncateDeterministically_ForLargeInput()
    {
        var report = CreateReport();
        var options = new AiPromptOptions
        {
            StrictMode = false,
            DefaultMaxPromptChars = 900,
            DefaultMaxPromptTokens = 225,
            TopMetricCount = 3
        };

        var result1 = AiPromptBuilder.BuildAnalysisPrompt(report, options, null);
        var result2 = AiPromptBuilder.BuildAnalysisPrompt(report, options, null);

        Assert.Equal(result1.Prompt, result2.Prompt);
        Assert.True(result1.Prompt.Length <= 900);
    }

    [Fact]
    public void BuildAnalysisPrompt_ShouldHandleMissingMetricFields()
    {
        var report = CreateReport(
            socialMetrics: [new ContextMetricDto("", "", null, null, null, "test")],
            crimeMetrics: []);

        var options = new AiPromptOptions { TopMetricCount = 2 };

        var result = AiPromptBuilder.BuildAnalysisPrompt(report, options, null);

        Assert.Contains("No high-confidence", result.Prompt);
        Assert.DoesNotContain("//", result.Prompt);
    }

    [Fact]
    public void BuildAnalysisPrompt_ShouldApplyStableTruncationInStrictMode()
    {
        var report = CreateReport();
        var options = new AiPromptOptions
        {
            StrictMode = true,
            DefaultMaxPromptChars = 500,
            DefaultMaxPromptTokens = 125
        };

        var result = AiPromptBuilder.BuildAnalysisPrompt(report, options, null);

        Assert.True(result.Prompt.Length <= 500);
        Assert.DoesNotContain('…', result.Prompt);
    }

    [Fact]
    public void BuildAnalysisPrompt_ShouldRedactAddressPrecision()
    {
        var report = CreateReport(displayAddress: "Prinsengracht 263, 1016 GV, Amsterdam, Noord-Holland");
        var options = new AiPromptOptions();

        var result = AiPromptBuilder.BuildAnalysisPrompt(report, options, null);

        Assert.DoesNotContain("263", result.Prompt);
        Assert.DoesNotContain("1016 GV", result.Prompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Amsterdam, Noord-Holland", result.Prompt);
    }

    private static ContextReportDto CreateReport(
        string displayAddress = "Damrak 123, 1012 LG Amsterdam",
        IReadOnlyList<ContextMetricDto>? socialMetrics = null,
        IReadOnlyList<ContextMetricDto>? crimeMetrics = null)
    {
        socialMetrics ??= Enumerable.Range(1, 40)
            .Select(i => new ContextMetricDto($"social_{i}", $"Social Metric {i}", i * 1.1, "pts", 40 + (i % 30), "test"))
            .ToList();

        crimeMetrics ??= Enumerable.Range(1, 40)
            .Select(i => new ContextMetricDto($"crime_{i}", $"Crime Metric {i}", i * 0.7, "idx", 20 + (i % 30), "test"))
            .ToList();

        var metrics = Enumerable.Range(1, 25)
            .Select(i => new ContextMetricDto($"metric_{i}", $"Metric {i}", i, "u", 30 + (i % 40), "test"))
            .ToList();

        return new ContextReportDto(
            new ResolvedLocationDto("query", displayAddress, 52.37, 4.89, null, null, null, null, null, null, null, null, "1012LG"),
            socialMetrics,
            crimeMetrics,
            metrics,
            metrics,
            metrics,
            metrics,
            metrics,
            72,
            new Dictionary<string, double>
            {
                ["Safety"] = 61,
                ["Social"] = 75,
                ["Amenities"] = 83,
                ["Mobility"] = 58
            },
            [],
            []);
    }
}
