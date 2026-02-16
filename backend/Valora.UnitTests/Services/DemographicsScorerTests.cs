using Valora.Application.Enrichment.Scoring;

namespace Valora.UnitTests.Services;

public class DemographicsScorerTests
{
    [Fact]
    public void ScoreFamilyFriendly_ReturnsNull_WhenAllInputsAreNull()
    {
        var score = DemographicsScorer.ScoreFamilyFriendly(null, null, null);
        Assert.Null(score);
    }

    [Theory]
    [InlineData(20.0, 15.0, 2.0, 50.0)] // Baseline
    [InlineData(30.0, 15.0, 2.0, 65.0)] // Family Households Boost: (30-20)*1.5 = 15. Total 65
    [InlineData(20.0, 20.0, 2.0, 60.0)] // Children Boost: (20-15)*2 = 10. Total 60
    [InlineData(20.0, 15.0, 3.0, 65.0)] // Household Size Boost: (3-2)*15 = 15. Total 65
    [InlineData(null, 15.0, 2.0, 50.0)] // Missing Family Households treated as neutral
    [InlineData(20.0, null, 2.0, 50.0)] // Missing Children treated as neutral
    [InlineData(20.0, 15.0, null, 50.0)] // Missing Size treated as neutral
    public void ScoreFamilyFriendly_CalculatesScoreCorrectly(double? pFamily, double? pChildren, double? avgSize, double expected)
    {
        var score = DemographicsScorer.ScoreFamilyFriendly(pFamily, pChildren, avgSize);
        Assert.Equal(expected, score);
    }

    [Fact]
    public void ScoreFamilyFriendly_ClampsTo100()
    {
        var score = DemographicsScorer.ScoreFamilyFriendly(100.0, 100.0, 10.0);
        Assert.Equal(100.0, score);
    }

    [Fact]
    public void ScoreFamilyFriendly_ClampsTo0()
    {
        // To get below 0: 50 + (0-20)*1.5 + (0-15)*2 + (1-2)*15
        // 50 - 30 - 30 - 15 = -25 -> should be 0
        var score = DemographicsScorer.ScoreFamilyFriendly(0.0, 0.0, 1.0);
        Assert.Equal(0.0, score);
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData(18.0, 0.0)]
    [InlineData(10.0, 0.0)] // (10-18)*6.5 = -52 -> 0
    [InlineData(28.0, 65.0)] // (28-18)*6.5 = 65
    [InlineData(40.0, 100.0)] // (40-18)*6.5 = 143 -> 100
    public void ScoreIncome_CalculatesCorrectly(double? income, double? expected)
    {
        var score = DemographicsScorer.ScoreIncome(income);
        Assert.Equal(expected, score);
    }

    [Theory]
    [InlineData(null, null, null, null)]
    [InlineData(10, 10, 10, 46.62)] // Share = 33.3 (rounded) * 1.4 = 46.62
    [InlineData(0, 0, 100, 100.0)] // Share = 100 * 1.4 = 140 -> 100
    public void ScoreEducation_CalculatesCorrectly(int? low, int? medium, int? high, double? expected)
    {
        var score = DemographicsScorer.ScoreEducation(low, medium, high);
        if (expected.HasValue)
        {
            Assert.NotNull(score);
            Assert.Equal(expected.Value, score.Value, 2);
        }
        else
        {
            Assert.Null(score);
        }
    }

    [Theory]
    [InlineData("zeer sterk stedelijk", 65.0)]
    [InlineData("sterk stedelijk", 85.0)]
    [InlineData("matig stedelijk", 100.0)]
    [InlineData("weinig stedelijk", 85.0)]
    [InlineData("niet stedelijk", 70.0)]
    [InlineData("unknown", null)]
    [InlineData(null, null)]
    public void ScoreUrbanity_CalculatesCorrectly(string? urbanity, double? expected)
    {
        var score = DemographicsScorer.ScoreUrbanity(urbanity);
        Assert.Equal(expected, score);
    }

    [Theory]
    [InlineData("zeer sterk stedelijk", 1.0)]
    [InlineData("sterk stedelijk", 2.0)]
    [InlineData("matig stedelijk", 3.0)]
    [InlineData("weinig stedelijk", 4.0)]
    [InlineData("niet stedelijk", 5.0)]
    [InlineData("1", 1.0)]
    [InlineData("5", 5.0)]
    [InlineData("UNKNOWN", null)]
    public void ParseUrbanityLevel_ParsesCorrectly(string? input, double? expected)
    {
        var result = DemographicsScorer.ParseUrbanityLevel(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(10, 10, 20, 25.0)] // 10 / 40 = 25%
    [InlineData(null, 10, 20, null)]
    [InlineData(10, null, 20, null)]
    [InlineData(0, 0, 0, null)]
    public void ToPercent_CalculatesCorrectly(int? target, int? one, int? two, double? expected)
    {
        var result = DemographicsScorer.ToPercent(target, one, two);
        Assert.Equal(expected, result);
    }
}
