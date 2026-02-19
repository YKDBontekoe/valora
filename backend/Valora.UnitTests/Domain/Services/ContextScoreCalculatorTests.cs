using Valora.Domain.Models;
using Valora.Domain.Services;
using Xunit;

namespace Valora.UnitTests.Domain.Services;

public class ContextScoreCalculatorTests
{
    [Fact]
    public void ComputeCategoryScores_AggregatesCorrectly()
    {
        // Arrange
        var social = new List<ContextMetricModel>
        {
            new("k1", "l1", 10, "u", 80, "src"),
            new("k2", "l2", 20, "u", 90, "src")
        };
        // Avg: 85

        var safety = new List<ContextMetricModel>
        {
             new("k3", "l3", 10, "u", 50, "src")
        };
        // Avg: 50

        var empty = new List<ContextMetricModel>();

        var input = new CategoryMetricsModel(
            social,
            safety,
            empty, // Demographics
            empty, // Housing
            empty, // Mobility
            empty, // Amenities
            empty  // Environment
        );

        // Act
        var result = ContextScoreCalculator.ComputeCategoryScores(input);

        // Assert
        Assert.Equal(85, result[ContextScoreCalculator.CategorySocial]);
        Assert.Equal(50, result[ContextScoreCalculator.CategorySafety]);
        Assert.False(result.ContainsKey(ContextScoreCalculator.CategoryDemographics));
    }

    [Fact]
    public void ComputeCompositeScore_WeightsCorrectly()
    {
        // Arrange
        // Social: 100 (Weight 0.20) -> 20
        // Safety: 50  (Weight 0.20) -> 10
        // Amenities: 80 (Weight 0.25) -> 20
        // Others: 0 (or missing)

        // Total Weight present: 0.20 + 0.20 + 0.25 = 0.65
        // Weighted Sum: 20 + 10 + 20 = 50
        // Expected Composite: 50 / 0.65 = 76.923...

        var scores = new Dictionary<string, double>
        {
            [ContextScoreCalculator.CategorySocial] = 100,
            [ContextScoreCalculator.CategorySafety] = 50,
            [ContextScoreCalculator.CategoryAmenities] = 80
        };

        // Act
        var result = ContextScoreCalculator.ComputeCompositeScore(scores);

        // Assert
        Assert.Equal(50 / 0.65, result, 4);
    }

    [Fact]
    public void ComputeCompositeScore_HandlesMissingCategories()
    {
        var scores = new Dictionary<string, double>();
        var result = ContextScoreCalculator.ComputeCompositeScore(scores);
        Assert.Equal(0, result);
    }
}
