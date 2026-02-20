using Valora.Domain.Services.Scoring;

namespace Valora.UnitTests.Domain.Services.Scoring;

public class ScoringRulesTests
{
    // Social Scoring Tests
    [Theory]
    [InlineData(100, 65.0)]   // Rural
    [InlineData(1000, 85.0)]  // Suburban
    [InlineData(3000, 100.0)] // Urban Optimal
    [InlineData(5000, 70.0)]  // Urban Dense
    [InlineData(8000, 50.0)]  // Overcrowded
    [InlineData(null, null)]
    public void ScoreDensity_ReturnsCorrectScore(int? density, double? expected)
    {
        Assert.Equal(expected, SocialScoringRules.ScoreDensity(density));
    }

    [Theory]
    [InlineData(0.0, 100.0)]
    [InlineData(5.0, 60.0)]
    [InlineData(10.0, 20.0)]
    [InlineData(15.0, 0.0)]  // > 12.5% becomes negative, clamped to 0
    [InlineData(null, null)]
    public void ScoreLowIncome_ReturnsCorrectScore(double? percent, double? expected)
    {
        Assert.Equal(expected, SocialScoringRules.ScoreLowIncome(percent));
    }

    [Theory]
    [InlineData(150.0, 0.0)]
    [InlineData(300.0, 50.0)]
    [InlineData(450.0, 100.0)]
    [InlineData(600.0, 100.0)] // Clamped to 100
    [InlineData(100.0, 0.0)]   // Clamped to 0
    [InlineData(null, null)]
    public void ScoreWoz_ReturnsCorrectScore(double? woz, double? expected)
    {
        Assert.Equal(expected, SocialScoringRules.ScoreWoz(woz));
    }

    // Crime Scoring Tests
    [Theory]
    [InlineData(10, 100.0)]
    [InlineData(30, 85.0)]
    [InlineData(45, 70.0)]
    [InlineData(60, 50.0)]
    [InlineData(90, 30.0)]
    [InlineData(150, 15.0)]
    [InlineData(null, null)]
    public void ScoreTotalCrime_ReturnsCorrectScore(int? crimes, double? expected)
    {
        Assert.Equal(expected, CrimeScoringRules.ScoreTotalCrime(crimes));
    }

    [Theory]
    [InlineData(1, 100.0)]
    [InlineData(4, 80.0)]
    [InlineData(8, 60.0)]
    [InlineData(12, 40.0)]
    [InlineData(20, 20.0)]
    [InlineData(null, null)]
    public void ScoreBurglary_ReturnsCorrectScore(int? burglary, double? expected)
    {
        Assert.Equal(expected, CrimeScoringRules.ScoreBurglary(burglary));
    }

    [Theory]
    [InlineData(1, 100.0)]
    [InlineData(4, 75.0)]
    [InlineData(8, 50.0)]
    [InlineData(12, 25.0)]
    [InlineData(null, null)]
    public void ScoreViolentCrime_ReturnsCorrectScore(int? violent, double? expected)
    {
        Assert.Equal(expected, CrimeScoringRules.ScoreViolentCrime(violent));
    }

    // Environment Scoring Tests
    [Theory]
    [InlineData(4.0, 100.0)]
    [InlineData(8.0, 85.0)]
    [InlineData(12.0, 70.0)]
    [InlineData(20.0, 50.0)]
    [InlineData(30.0, 25.0)]
    [InlineData(40.0, 10.0)]
    [InlineData(null, null)]
    public void ScorePm25_ReturnsCorrectScore(double? pm25, double? expected)
    {
        Assert.Equal(expected, EnvironmentScoringRules.ScorePm25(pm25));
    }

    [Theory]
    [InlineData(10.0, 100.0)]
    [InlineData(20.0, 85.0)]
    [InlineData(30.0, 70.0)]
    [InlineData(40.0, 50.0)]
    [InlineData(50.0, 30.0)]
    [InlineData(70.0, 15.0)]
    [InlineData(null, null)]
    public void ScorePm10_ReturnsCorrectScore(double? pm10, double? expected)
    {
        Assert.Equal(expected, EnvironmentScoringRules.ScorePm10(pm10));
    }

    [Theory]
    [InlineData(10.0, 100.0)]
    [InlineData(25.0, 85.0)]
    [InlineData(35.0, 70.0)]
    [InlineData(50.0, 50.0)]
    [InlineData(70.0, 30.0)]
    [InlineData(90.0, 15.0)]
    [InlineData(null, null)]
    public void ScoreNo2_ReturnsCorrectScore(double? no2, double? expected)
    {
        Assert.Equal(expected, EnvironmentScoringRules.ScoreNo2(no2));
    }

    [Theory]
    [InlineData(40.0, 100.0)]
    [InlineData(80.0, 85.0)]
    [InlineData(110.0, 70.0)]
    [InlineData(140.0, 50.0)]
    [InlineData(170.0, 30.0)]
    [InlineData(200.0, 15.0)]
    [InlineData(null, null)]
    public void ScoreO3_ReturnsCorrectScore(double? o3, double? expected)
    {
        Assert.Equal(expected, EnvironmentScoringRules.ScoreO3(o3));
    }
}
