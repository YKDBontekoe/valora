using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Xunit;

namespace Valora.UnitTests.Enrichment;

public class ContextScoreCalculatorTests
{
    [Fact]
    public void ComputeCategoryScore_ShouldReturnAverage()
    {
        var metrics = new List<ContextMetricDto>
        {
            new("m1", "M1", 10, "unit", 80, "source"),
            new("m2", "M2", 20, "unit", 90, "source"),
            new("m3", "M3", 30, "unit", null, "source") // Null score should be ignored
        };

        var score = ContextScoreCalculator.ComputeCategoryScore(metrics);

        Assert.Equal(85, score);
    }

    [Fact]
    public void ComputeCategoryScore_WithNoScores_ShouldReturnNull()
    {
        var metrics = new List<ContextMetricDto>
        {
            new("m1", "M1", 10, "unit", null, "source")
        };

        var score = ContextScoreCalculator.ComputeCategoryScore(metrics);

        Assert.Null(score);
    }

    [Fact]
    public void ComputeCompositeScore_ShouldHandleWeights()
    {
        var categoryScores = new Dictionary<string, double>
        {
            [ContextScoreCalculator.CategorySocial] = 80,
            [ContextScoreCalculator.CategorySafety] = 90
        };

        var composite = ContextScoreCalculator.ComputeCompositeScore(categoryScores);

        // (80 * 0.2 + 90 * 0.2) / (0.2 + 0.2) = 85
        Assert.Equal(85, composite);
    }
}
